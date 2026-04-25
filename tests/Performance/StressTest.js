import http from 'k6/http';
import { check } from 'k6';

// 1. PRE-GENERATE IDs (Done once at startup, not during the test)
const idPool = Array.from({ length: 10000 }, (_, i) => `id-${i}`);

export const options = {
  scenarios: {
    clean_performance_run: {
      executor: 'constant-arrival-rate',
      rate: 50000,           // Target 50k RPS
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 500,  // Efficient thread count for Ryzen 9
      maxVUs: 1000,          // Small buffer for spikes
    },
  },
  thresholds: {
    http_req_duration: ['p(99) < 15'], 
    http_req_failed: ['rate < 0.001'],
  },
};


// 2. GLOBAL COUNTER (Fastest way to get unique data)
let counter = 0;

export default function () {
  // Use bitwise OR to keep counter in a small range if needed
  const id = idPool[counter % idPool.length];
  counter++;

  // Static payload is faster than JSON.stringify every time
  const payload = `{"id":"${id}","data":"sample-data"}`;

  const params = {
    headers: { 'Content-Type': 'application/json' },
    // 3. TAGGING (Helps analyze results without overhead)
    tags: { name: 'IngestPost' },
  };

  const res = http.post('http://localhost:5000/ingest', payload, params);

  check(res, {
    'status is 200': (r) => r.status >= 200 && r.status < 300,
    'latency is acceptable': (r) => r.timings.duration < 100, // Higher ceiling
  });

  //sleep(1); // Adjust sleep time as needed for load balancing k6 testing. 
  // DEACTIVATED: We use 'constant-arrival-rate' instead of 'looping VUs'.
  // This allows k6 to manage request pacing for 100K RPS without 
  // the CPU overhead of thousands of idle threads.
}
