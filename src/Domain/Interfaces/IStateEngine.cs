namespace EventEngine.Api.Domain.Interfaces
{
    public interface IStateEngine<TState, TTrigger>
    {
        // Ensures state transitions are deterministic and auditable
        TState Transition(TState currentState, TTrigger trigger);
        bool IsValidTransition(TState current, TState next);
    }
}
