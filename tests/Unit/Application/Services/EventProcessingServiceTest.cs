// tests/Unit/Application/Services/EventProcessingServiceTests.cs
using EventEngine.Api.Application.Services;
using EventEngine.Api.Domain.Interfaces;
using EventEngine.Api.Domain.State;
using EventEngine.Api.src.Domain.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Unit.Application.Services;

public class EventProcessingServiceTests
{
    private readonly Mock<IEventIngestor<string>> _eventIngestorMock;
    private readonly Mock<IStateEngine<EventState, Trigger>> _stateEngineMock;
    private readonly EventProcessingService<string> _service;

    public EventProcessingServiceTests()
    {
        _eventIngestorMock = new Mock<IEventIngestor<string>>();
        _stateEngineMock = new Mock<IStateEngine<EventState, Trigger>>();

        _service = new EventProcessingService<string>(_eventIngestorMock.Object, _stateEngineMock.Object);
    }

    [Fact]
    public async Task StressTest_HighConcurrency_Should_Not_Throw()
    {
        const int numberOfTasks = 10000;
        var tasks = new List<Task>();

        for (int i = 0; i < numberOfTasks; i++)
        {
            tasks.Add(_service.ProcessEventAsync($"event-{i}", CancellationToken.None));
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task IdempotencyTest_SameEvent_Should_Not_Change_State()
    {
        const string eventId = "same-event";
        _stateEngineMock.Setup(se => se.Transition(It.IsAny<EventState>(), It.IsAny<Trigger>()))
            .Returns(EventState.Persisted);

        await _service.ProcessEventAsync(eventId, CancellationToken.None);
        await _service.ProcessEventAsync(eventId, CancellationToken.None);

        _eventIngestorMock.Verify(ei => ei.IngestAsync(eventId, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task BackpressureTest_Should_Handle_CapacityExceeded()
    {
        const string eventId = "backpressure-event";
        _eventIngestorMock.Setup(ei => ei.CapacityExceeded).Returns(true);

        await _service.ProcessEventAsync(eventId, CancellationToken.None);

        _stateEngineMock.Verify(se => se.Transition(It.IsAny<EventState>(), It.IsAny<Trigger>()), Times.Never());
    }
}