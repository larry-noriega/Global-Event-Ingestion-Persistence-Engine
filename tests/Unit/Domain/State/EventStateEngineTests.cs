// tests/Unit/Domain/State/EventStateEngineTests.cs
using System;
using EventEngine.Api.Domain.State;
using Xunit;

namespace Unit.Domain.State;

public class EventStateEngineTests
{
    private readonly EventStateEngine _stateEngine;

    public EventStateEngineTests()
    {
        _stateEngine = new EventStateEngine();
    }

    [Theory]
    [InlineData(EventState.Received, Trigger.Validate, EventState.Validated)]
    [InlineData(EventState.Validated, Trigger.Persist, EventState.Persisted)]
    public void Transition_Valid_Transitions_Should_Succeed(EventState currentState, Trigger trigger, EventState expected)
    {
        var nextState = _stateEngine.Transition(currentState, trigger);
        Assert.Equal(expected, nextState);
    }

    [Theory]
    [InlineData(EventState.Received, Trigger.Persist)] // Invalid transition
    [InlineData(EventState.Validated, Trigger.Validate)] // Invalid transition
    public void Transition_Invalid_Transitions_Should_Throw(EventState currentState, Trigger trigger)
    {
        Assert.Throws<InvalidOperationException>(() => _stateEngine.Transition(currentState, trigger));
    }

    [Fact]
    public void IsValidTransition_Valid_Transitions_Should_Return_True()
    {
        var result = _stateEngine.IsValidTransition(EventState.Validated, EventState.Persisted);
        Assert.True(result);
    }

    [Fact]
    public void IsValidTransition_Invalid_Transitions_Should_Return_False()
    {
        var result = _stateEngine.IsValidTransition(EventState.Received, EventState.Persisted);
        Assert.False(result);
    }

    [Fact]
    public async Task Transition_ShouldBeThreadSafe_UnderHighConcurrency()
    {
        const int taskCount = 100;
        var tasks = new Task<EventState>[taskCount];
        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() => _stateEngine.Transition(EventState.Received, Trigger.Validate)); // Simulate simultaneous valid transitions
        }

        var results = await Task.WhenAll(tasks);
        Assert.All(results, state => Assert.Equal(EventState.Validated, state));
    }

}