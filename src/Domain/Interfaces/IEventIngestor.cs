using System.Threading;
using System.Threading.Tasks;

namespace EventEngine.Api.src.Domain.Interfaces;

public interface IEventIngestor<TEvent>
{
    // Async task focused on sub-millisecond ingestion
    Task IngestAsync(TEvent payload, CancellationToken ct);

    // Handles backpressure when the system is saturated
    bool CapacityExceeded { get; }
}

