using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using Oakton.Descriptions;
using Spectre.Console;
using Wolverine.Configuration;
using Wolverine.Runtime.Routing;
using Wolverine.Transports;
using Wolverine.Transports.Local;

namespace Wolverine;

public partial class WolverineOptions : IEnumerable<ITransport>, IAsyncDisposable
{
    private readonly Dictionary<string, ITransport> _transports = new();

    public async ValueTask DisposeAsync()
    {
        foreach (var transport in _transports.Values)
        {
            if (transport is IAsyncDisposable ad)
            {
                await ad.DisposeAsync();
            }
            else if (transport is IDisposable d)
            {
                d.Dispose();
            }
        }
    }

    internal IList<IMessageRoutingConvention> RoutingConventions { get; } = new List<IMessageRoutingConvention>();

    /// <summary>
    /// Register a routing convention that Wolverine will use to discover and apply
    /// message routing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T RouteWith<T>() where T : IMessageRoutingConvention, new()
    {
        var convention = new T();
        RouteWith(convention);

        return convention;
    }

    /// <summary>
    /// Register a routing convention that Wolverine will use to discover and apply
    /// message routing
    /// </summary>
    /// <param name="routingConvention"></param>
    public void RouteWith(IMessageRoutingConvention routingConvention)
    {
        RoutingConventions.Add(routingConvention);
    }



    /// <summary>
    ///     Directs Wolverine to set up an incoming listener for the given Uri
    /// </summary>
    /// <param name="uri"></param>
    public IListenerConfiguration ListenForMessagesFrom(Uri uri)
    {
        var settings = findTransport(uri).ListenTo(uri);
        return new ListenerConfiguration(settings);
    }

    /// <summary>
    ///     Directs Wolverine to set up an incoming listener for the given Uri
    /// </summary>
    public IListenerConfiguration ListenForMessagesFrom(string uriString)
    {
        return ListenForMessagesFrom(new Uri(uriString));
    }


    public void Publish(Action<PublishingExpression> configuration)
    {
        var expression = new PublishingExpression(this);
        configuration(expression);
        expression.AttachSubscriptions();
    }

    /// <summary>
    /// Create a sending endpoint with no subscriptions. This
    /// can be useful for programmatic sending to named endpoints
    /// </summary>
    /// <returns></returns>
    public PublishingExpression Publish()
    {
        return new PublishingExpression(this);
    }

    /// <summary>
    /// Shorthand syntax to route a single message type
    /// </summary>
    /// <typeparam name="TMessageType"></typeparam>
    /// <returns></returns>
    public PublishingExpression PublishMessage<TMessageType>()
    {
        var expression = new PublishingExpression(this)
        {
            AutoAddSubscriptions = true

        };

        expression.Message<TMessageType>();

        return expression;
    }

    public IPublishToExpression PublishAllMessages()
    {
        var expression = new PublishingExpression(this)
        {
            AutoAddSubscriptions = true
        };

        expression.AddSubscriptionForAllMessages();
        return expression;
    }

    public IListenerConfiguration LocalQueue(string queueName)
    {
        var settings = GetOrCreate<LocalTransport>().QueueFor(queueName);
        return new ListenerConfiguration(settings);
    }

    public IListenerConfiguration DefaultLocalQueue => LocalQueue(TransportConstants.Default);
    public IListenerConfiguration DurableScheduledMessagesLocalQueue => LocalQueue(TransportConstants.Durable);

    public void StubAllExternallyOutgoingEndpoints()
    {
        Advanced.StubAllOutgoingExternalSenders = true;
    }

    public IEnumerator<ITransport> GetEnumerator()
    {
        return _transports.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    Task IWriteToConsole.WriteToConsole()
    {
        var tree = new Tree("Transports and Endpoints");

        foreach (var transport in _transports.Values.Where(x => x.Endpoints().Any()))
        {
            var transportNode = tree.AddNode($"[bold]{transport.Name}[/] [dim]({transport.Protocols.Join(", ")}[/])");
            if (transport is ITreeDescriber d)
            {
                d.Describe(transportNode);
            }

            foreach (var endpoint in transport.Endpoints())
            {
                var endpointTitle = endpoint.Uri.ToString();
                if (endpoint.IsUsedForReplies || ReferenceEquals(endpoint, transport.ReplyEndpoint()))
                {
                    endpointTitle += " ([bold]Used for Replies[/])";
                }

                var endpointNode = transportNode.AddNode(endpointTitle);

                if (endpoint.IsListener)
                {
                    endpointNode.AddNode("[bold green]Listener[/]");
                }

                var props = endpoint.DescribeProperties();
                if (props.Any())
                {
                    var table = props.BuildTableForProperties();

                    endpointNode.AddNode(table);
                }

                if (endpoint.Subscriptions.Any())
                {
                    var subscriptions = endpointNode.AddNode("Subscriptions");
                    foreach (var subscription in endpoint.Subscriptions)
                        subscriptions.AddNode($"{subscription} ({subscription.ContentTypes.Join(", ")})");
                }
            }
        }

        AnsiConsole.Render(tree);

        return Task.CompletedTask;
    }

    public ITransport? TransportForScheme(string scheme)
    {
        return _transports.TryGetValue(scheme.ToLowerInvariant(), out var transport)
            ? transport
            : null;
    }

    public void Add(ITransport transport)
    {
        foreach (var protocol in transport.Protocols) _transports.SmartAdd(protocol, transport);
    }

    public T GetOrCreate<T>() where T : ITransport, new()
    {
        var transport = _transports.Values.OfType<T>().FirstOrDefault();
        if (transport == null)
        {
            transport = new T();
            foreach (var protocol in transport.Protocols) _transports[protocol] = transport;
        }

        return transport;
    }

    public Endpoint? TryGetEndpoint(Uri uri)
    {
        return findTransport(uri).TryGetEndpoint(uri);
    }

    private ITransport findTransport(Uri uri)
    {
        var transport = TransportForScheme(uri.Scheme);
        if (transport == null)
        {
            throw new InvalidOperationException($"Unknown Transport scheme '{uri.Scheme}'");
        }

        return transport;
    }

    public Endpoint GetOrCreateEndpoint(Uri uri)
    {
        return findTransport(uri).GetOrCreateEndpoint(uri);
    }

    public Endpoint[] AllEndpoints()
    {
        return _transports.Values.SelectMany(x => x.Endpoints()).ToArray();
    }
}
