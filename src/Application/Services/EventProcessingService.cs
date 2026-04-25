// src/Application/Services/EventProcessingService.cs
using System.Collections.Concurrent;
using EventEngine.Api.src.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using EventEngine.Api.Domain.State;
using EventEngine.Api.Domain.Interfaces;

namespace EventEngine.Api.Application.Services;

public class EventProcessingService<T>
{
    private readonly IEventIngestor<T> _eventIngestor;
    private readonly IStateEngine<EventState, Trigger> _stateEngine;
    
    // Store processed IDs to ensure idempotency
    private readonly ConcurrentDictionary<T, byte> _processedEvents = new();

    public EventProcessingService(IEventIngestor<T> ingestor, IStateEngine<EventState, Trigger> stateEngine)
    {
        _eventIngestor = ingestor;
        _stateEngine = stateEngine;
    }

    public async Task ProcessEventAsync(T eventId, CancellationToken ct)
    {
        // 1. Check Backpressure first (Performance optimization)
        if (_eventIngestor.CapacityExceeded)
        {
            return; 
        }

        // 2. Idempotency Check: Try to add the ID. If it exists, skip.
        if (!_processedEvents.TryAdd(eventId, 0))
        {
            return; // Exit early: Event already processed
        }

        // 3. Logic Execution
        // In a real scenario, you'd fetch current state. Here we assume 'Received'.
        _stateEngine.Transition(EventState.Received, Trigger.Validate);

        await _eventIngestor.IngestAsync(eventId, ct);
    }
}
