// src/Domain/State/EventStateEngine.cs
using EventEngine.Api.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace EventEngine.Api.Domain.State;

public enum EventState { Received, Validated, Persisted }
public enum Trigger { Validate, Persist }

public class EventStateEngine : IStateEngine<EventState, Trigger>
{
    private readonly Dictionary<(EventState, Trigger), EventState> _transitions = new()
        {
            { (EventState.Received, Trigger.Validate), EventState.Validated },
            { (EventState.Validated, Trigger.Persist), EventState.Persisted }
        };

    public EventState Transition(EventState currentState, Trigger trigger)
    {
        if (_transitions.TryGetValue((currentState, trigger), out var nextState))
        {
            return nextState;
        }
        throw new InvalidOperationException($"Invalid transition from {currentState} with trigger {trigger}");
    }

    public bool IsValidTransition(EventState current, EventState next)
    {
        return (current, next) switch
        {
            (EventState.Received, EventState.Validated) => true,
            (EventState.Validated, EventState.Persisted) => true,
            _ => false // This ensures Received -> Persisted returns False
        };
    }

}
