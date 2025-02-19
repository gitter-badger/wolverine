// <auto-generated/>
#pragma warning disable
using Wolverine.Persistence.Marten.Publishing;

namespace Internal.Generated.WolverineHandlers
{
    // START: IncrementCHandler2087566069
    public class IncrementCHandler2087566069 : Wolverine.Runtime.Handlers.MessageHandler
    {
        private readonly Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;

        public IncrementCHandler2087566069(Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory)
        {
            _outboxedSessionFactory = outboxedSessionFactory;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Wolverine.IMessageContext context, System.Threading.CancellationToken cancellation)
        {
            var letterHandler = new Wolverine.Persistence.Testing.Marten.LetterHandler();
            var incrementC = (Wolverine.Persistence.Testing.Marten.IncrementC)context.Envelope.Message;
            await using var documentSession = _outboxedSessionFactory.OpenSession(context);
            var eventStore = documentSession.Events;
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<Wolverine.Persistence.Testing.Marten.LetterAggregate>(incrementC.LetterAggregateId, cancellation).ConfigureAwait(false);

            letterHandler.Handle(incrementC, eventStream);
            await documentSession.SaveChangesAsync(cancellation).ConfigureAwait(false);
        }

    }

    // END: IncrementCHandler2087566069
    
    
}

