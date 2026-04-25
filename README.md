# Distributed High-Throughput Event Engine (100K+ RPS)

<img width="1091" height="910" alt="Capture d’écran du 2026-04-25 01-38-33" src="https://github.com/user-attachments/assets/5ff031fb-0438-4570-bf30-d62ad6945897" />

## Executive Summary
This project is a high-performance architectural blueprint designed to ingest, validate, and persist massive streams of telemetry data. It demonstrates **System Reliability**, **Data Integrity**, and **Scalable Throughput** through a tech-agnostic, distributed approach.

The core objective is to handle an ingestion rate of **100,000+ Requests Per Second (RPS)** while ensuring 100% data consistency through a decoupled, event-driven architecture.

## Performance Benchmarks (Verified)
*   **Peak Throughput:** 111,000+ operations/sec.
*   **Sustained Throughput:** 50,000+ RPS (Verified on a single node).
*   **P99 Latency:** **14.51ms** under peak concurrency.
*   **Median Latency:** **69μs** (Microsecond-level core processing).
*   **Error Rate:** 0.00% (Zero failures across 2.8M+ requests in stress tests).

## Architectural Overview
The system utilizes a **Hexagonal (Ports & Adapters) Architecture** to isolate business logic from infrastructure, ensuring the system remains modular and highly testable.

### **Key Pillars:**
*   **Ingestion Layer:** Load-balanced entry points optimized for sub-millisecond request processing.
*   **Stateful Validation:** A deterministic finite state machine (FSM) governed by an `IStateEngine`, ensuring no illegal data states are persisted.
*   **Lock-Free Concurrency:** Replaced standard mutexes with atomic CPU instructions (`Interlocked`) to eliminate thread contention and context-switching overhead.
*   **Asynchronous Buffering:** Decouples high-speed ingestion from persistence layers to handle traffic spikes without backpressure.

## Strategic Decision Log
*   **Why Lock-Free?** Standard `lock` blocks caused "Thread Stampedes" and context switching at scale. Switching to `Interlocked` atomic operations reduced p99 latency from ~2s to <15ms.
*   **Why FSM?** To eliminate race conditions and ensure state transitions are predictable and auditable at scale.
*   **Mechanical Sympathy:** Optimized for AMD Ryzen 9 8945HS architecture by utilizing **Server GC** and high-concurrency Linux kernel socket tuning (TCP Reuse / File Descriptor limits).

## System Design Patterns
*   **Event Sourcing:** Maintains an immutable audit trail of every state change for full system transparency.
*   **Dependency Injection:** Employed to ensure the system is fully decoupled and maintainable.
*   **Minimal API Ingestion:** Leverages the high-performance .NET Minimal API pipeline to reduce request overhead compared to traditional Controllers.

## Core Logic Implementation

### State Machine Implementation
The `EventStateEngine` in `/src/Domain/State` handles transitions from `Received` to `Validated` to `Persisted`. The FSM ensures strict state integrity, rejecting illegal transitions via a deterministic logic gate.

### Event Processing Service
The `EventProcessingService` orchestrates the flow using a non-blocking ingestion strategy. It checks `CapacityExceeded` via `Volatile` reads, ensuring the system remains responsive even at the 100K RPS threshold.

## Testing Strategy

### Unit Tests for EventStateEngine
- **State Integrity Tests:** Verify that the `EventStateEngine` strictly rejects illegal transitions using a theory-based approach to test multiple valid and invalid state sequences.
- **Thread Safety Tests:** Ensure thread-safe state transitions.

### Concurrency Tests for EventProcessingService
- **High-Concurrency Simulation:** Write a test case that simulates a "Burst Load" using `Task.WhenAll` to trigger 10,000+ simultaneous state transitions to ensure the FSM remains thread-safe and deterministic.
- **Idempotency Validation:** Ensure that processing the same Event ID twice does not lead to duplicate state changes or inconsistent data—this is critical for our 100K+ RPS data integrity requirement.
- **Mocking Infrastructure:** Use a mocking framework (like Moq) to simulate the `IEventIngestor` and ensure the service correctly handles "Backpressure" signals when the mock ingestor returns a CapacityExceeded state.

### Stress Test for EventProcessingService
- Measure execution time under high concurrency to verify low-latency logic.

## Performance Testing Strategy

### Load Testing Script (k6)

* **Tool Choice:** Grafana k6 (JavaScript)
*   **Executor:** `constant-arrival-rate` to decouple request frequency from Virtual User (VU) overhead.
*   **Scenario:** 
    *   Target: 50,000 RPS sustained.
    *   Pre-allocated VUs: 500 (optimized for 16-thread CPU utilization).
*   **Environment:** Verified on Ubuntu 24.04 LTS (Kernel 6.17).
- **Validation Logic:**
  - SLA Threshold (P99 < 15ms): It ensures that 99% of all traffic is processed within our target window.
  - Functional Check (Latency < 100ms): We implement a "Higher Ceiling" within the execution loop just to identify extreme outliers caused by "Cold Starts," OS background interrupts, or TCP re-transmissions without triggering a false-negative for the entire run.
  - Verify a 0% failure rate (HTTP 200 or successful internal processing).
  - Implement correlation IDs in the payload to ensure we can track data integrity across the flow.
  
### Execution Modes
*   **Constant Arrival Rate Test (Recommended):**
  Uses the internal k6 `constant-arrival-rate` executor for stable, high-throughput testing.
  ```sh
  k6 run tests/Performance/StressTest.js
  ```
*   **Multiple Instances Test:**
  To distribute load across a cluster, use the `--vus` override. 
  *Note: Ensure the `sleep(1)` line in the script is active when using this mode to pace requests correctly.*
  ```sh
  # Example for 10,000 concurrent workers per node
  k6 run --vus 10000 tests/Performance/StressTest.js
  ```

## How to Reproduce
1.  **Build:** 
    ```sh
    dotnet publish -c Release -r linux-x64 --self-contained true
    ```
2.  **Run:**
    ```sh
    COMPlus_gcServer=1 COMPlus_GCLatencyMode=1 ./EventEngine.Api
    ```
3.  **Test:**
    ```sh
    k6 run tests/Performance/StressTest.js
    ```


## Folder Structure
```text
/src
  /Domain                 # Pure business logic (Models, FSM, Interfaces)
  /Application            # Use cases and Service Orchestration
  /Infrastructure         # Adapters (Ingestion, Persistence, Messaging)
  /Common                 # Cross-cutting concerns (Logging, DI)
/tests
  /Unit                   # Domain and Logic tests
  /Performance            # k6 High-concurrency stress scripts
```

---
**Maintained by:** Larry Noriega 
*Senior Technical Lead & Distributed Systems Architect*
