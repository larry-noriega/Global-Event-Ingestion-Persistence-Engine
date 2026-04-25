// src/Infrastructure/Ingestion/EventIngestor.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using EventEngine.Api.src.Domain.Interfaces;

namespace EventEngine.Api.Infrastructure.Ingestion;

public class EventIngestor : IEventIngestor<string>
{
    // Use an atomic counter (Thread-safe without locking)
    private int _currentLoad = 0;
    private const int MaxCapacity = 100000;

    // Direct check without a lock
    public bool CapacityExceeded => Volatile.Read(ref _currentLoad) >= MaxCapacity;

    public Task IngestAsync(string payload, CancellationToken ct)
    {
        // 1. Atomic Increment (CPU-level efficiency)
        int newLoad = Interlocked.Increment(ref _currentLoad);

        // 2. Non-blocking capacity check
        if (newLoad > MaxCapacity)
        {
            Interlocked.Decrement(ref _currentLoad); // Rollback
            throw new InvalidOperationException("Capacity exceeded");
        }

        // 3. Return a cached completed task (Avoids allocation)
        return Task.CompletedTask;
    }
}
