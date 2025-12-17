using MassTransit;
using Play.Trading.Service.Activities;
using Play.Trading.Service.Contracts;

namespace Play.Trading.Service.StatesMachine
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        public State Accepted { get; set; }

        public State ItemsGranted { get; set; }

        public State Completed { get; set; }

        public State Faulted { get; set; }

        public Event<PurchaseRequested> PurchaseRequested { get; }
        public Event<GetPurchaseState> GetPurchaseState { get; }

        public Event<Fault<PurchaseRequested>> PurchaseRequestedFaulted { get; private set; }

        public PurchaseStateMachine()
        {
            InstanceState(state => state.CurrentState);
            ConfigureEvents();
            ConfigureInitialState();
            ConfigureAny();
        }

        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => GetPurchaseState, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PurchaseRequestedFaulted, x => x.CorrelateById(context => context.Message.Message.CorrelationId));
        }

        private void ConfigureInitialState()
        {
            Initially(
                When(PurchaseRequested)
                .Then(context =>
                {
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.ItemId = context.Message.ItemId;
                    context.Saga.Quantity = context.Message.Quantity;
                    context.Saga.Received = DateTimeOffset.Now;
                    context.Saga.LastUpdated = context.Saga.Received;
                })
                .Activity(x=> x.OfType<CalculatePurchaseTotalActivity>())
                .TransitionTo(Accepted));
        }

        private void ConfigureAny()
        {
            // Respond to queries
            DuringAny(
                When(GetPurchaseState)
                    .Respond(x => x.Saga)
            );

            // Capture faults globally
            DuringAny(
                When(PurchaseRequestedFaulted) // this is the built-in fault event
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = string.Join(",",context.Message.Exceptions.Select(c=> c.Message));
                        context.Saga.LastUpdated = DateTimeOffset.UtcNow;
                    })
            );

        }
    }
}
