// <auto-generated/>
#pragma warning disable
using Microsoft.Extensions.Logging;
using Wolverine.Persistence.Marten.Publishing;

namespace Internal.Generated.WolverineHandlers
{
    // START: IncrementBHandler2087566070
    public class IncrementBHandler2087566070 : Wolverine.Runtime.Handlers.MessageHandler
    {
        private readonly Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<Wolverine.Persistence.Testing.Marten.LetterHandler> _logger;

        public IncrementBHandler2087566070(Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory, Microsoft.Extensions.Logging.ILogger<Wolverine.Persistence.Testing.Marten.LetterHandler> logger)
        {
            _outboxedSessionFactory = outboxedSessionFactory;
            _logger = logger;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Wolverine.IMessageContext context, System.Threading.CancellationToken cancellation)
        {
            var letterHandler = new Wolverine.Persistence.Testing.Marten.LetterHandler();
            var incrementB = (Wolverine.Persistence.Testing.Marten.IncrementB)context.Envelope.Message;
            await using var documentSession = _outboxedSessionFactory.OpenSession(context);
            var eventStore = documentSession.Events;
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<Wolverine.Persistence.Testing.Marten.LetterAggregate>(incrementB.LetterAggregateId, cancellation).ConfigureAwait(false);

            var bEvent = await letterHandler.Handle(incrementB, eventStream.Aggregate, _logger).ConfigureAwait(false);
            if (bEvent != null)
            {
                // Capturing any possible events returned from the command handlers
                eventStream.AppendOne(bEvent);

            }

            await documentSession.SaveChangesAsync(cancellation).ConfigureAwait(false);
        }

    }

    // END: IncrementBHandler2087566070
    
    
}

