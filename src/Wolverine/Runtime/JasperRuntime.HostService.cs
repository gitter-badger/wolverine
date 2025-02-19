using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Lamar;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using Wolverine.Persistence.Durability;
using Wolverine.Runtime.Scheduled;
using Wolverine.Runtime.WorkerQueues;
using Wolverine.Transports;
using Wolverine.Transports.Local;

namespace Wolverine.Runtime;

public partial class WolverineRuntime
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Build up the message handlers
            await Handlers.CompileAsync(Options, _container);

            await startMessagingTransportsAsync();

            startInMemoryScheduledJobs();

            await startDurabilityAgentAsync();
        }
        catch (Exception? e)
        {
            MessageLogger.LogException(e, message: "Failed to start the Wolverine messaging");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hasStopped)
        {
            return;
        }

        _hasStopped = true;

        // This is important!
        _container.As<Container>().DisposalLock = DisposalLock.Unlocked;

        if (Durability != null)
        {
            await Durability.StopAsync(cancellationToken);
        }

        // Drain the listeners
        foreach (var listener in _listeners.Values)
        {
            try
            {
                await listener.StopAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to 'drain' outstanding messages in listener {Uri}", listener.Uri);
            }
        }

        Advanced.Cancel();
    }

    private void startInMemoryScheduledJobs()
    {
        ScheduledJobs =
            new InMemoryScheduledJobProcessor((ILocalQueue)AgentForLocalQueue(TransportConstants.Replies));

        // Bit of a hack, but it's necessary. Came up in compliance tests
        if (Persistence is NullEnvelopePersistence p)
        {
            p.ScheduledJobs = ScheduledJobs;
        }
    }

    private async Task startMessagingTransportsAsync()
    {
        foreach (var transport in Options)
        {
            await transport.InitializeAsync(this).ConfigureAwait(false);
            foreach (var endpoint in transport.Endpoints()) endpoint.Runtime = this; // necessary to locate serialization
        }

        // Let any registered routing conventions discover listener endpoints
        foreach (var routingConvention in Options.RoutingConventions)
        {
            routingConvention.DiscoverListeners(this, Handlers.Chains.Select(x => x.MessageType).ToList());
        }

        foreach (var transport in Options)
        {
            transport.StartSenders(this);
        }

        var listeningEndpoints = Options.SelectMany(x => x.Endpoints())
            .Where(x => x.IsListener).Where(x => x is not LocalQueueSettings);

        foreach (var endpoint in listeningEndpoints)
        {
            var agent = new ListeningAgent(endpoint, this);
            await agent.StartAsync().ConfigureAwait(false);
            _listeners[agent.Uri] = agent;
        }
    }

    private async Task startDurabilityAgentAsync()
    {
        // HOKEY, BUT IT WORKS
        if (_container.Model.DefaultTypeFor<IEnvelopePersistence>() != typeof(NullEnvelopePersistence) &&
            Options.Advanced.DurabilityAgentEnabled)
        {
            var durabilityLogger = _container.GetInstance<ILogger<DurabilityAgent>>();

            // TODO -- use the worker queue for Retries?
            var worker = new DurableReceiver(new LocalQueueSettings("scheduled"), this, Pipeline);

            Durability = new DurabilityAgent(this, Logger, durabilityLogger, worker, Persistence,
                Options.Advanced);

            await Durability.StartAsync(Options.Advanced.Cancellation);
        }
    }
}
