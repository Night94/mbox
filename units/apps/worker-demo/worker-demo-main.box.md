---
mbox_unit: 1
unit: worker-demo-main
type: box
version: 1
uses:
  box: 5
  worker-api: 1
---

# worker-demo-main

The entry box for the `worker-demo` app. Exercises framework features against the `worker-api` interface: parallel handlers, request timeout, declared failure, and explicit box creation and destruction.

## Definition

```yaml
provides: []
consumes:
  - interface: worker-api
    operations: [add, slow-add, divide]
configuration: {}
sideEffects: []
```

## Responsibility boundary

Owns only its own demonstration sequence. It does not own arithmetic semantics (the `worker-api` provider does) or the framework's concurrency or timeout mechanics.

## State assumptions

Stateless across invocations. Its `run` invocation executes a fixed scripted sequence and returns.

## Failure behavior

The sequence intentionally provokes a timeout and a declared `divide-by-zero` failure and treats both as expected outcomes. Other operation failures propagate as exceptions from `run`.

## Startup behavior

The `run` invocation:

1. Creates two additional worker instances via the framework's `createBox`, yielding `worker|1` and `worker|2`.
2. Sends three `slow-add` `REQ`s to `worker|1` in parallel, each with `delayMs: 800`. Measures and logs elapsed time to confirm parallel handler execution.
3. Sends one `slow-add` to `worker|1` with `delayMs: 3000` and a per-call `timeout: 500`. Expects the call to raise a local timeout.
4. Sends `divide { a: 10, b: 0 }` to `worker|1`. Expects `status: error, text: divide-by-zero`.
5. Sends `add { a: 2, b: 3 }` to `worker|2`. Expects `status: ok` with `sum: 5`.
6. Destroys `worker|1` and `worker|2`.
7. Requests application shutdown.

## Test expectations

Total elapsed time for step 2 is close to a single `delayMs`, not three. Step 3 raises a local timeout. Step 4 returns a declared `divide-by-zero` failure. Step 5 returns the correct sum. Steps 6 and 7 complete cleanly.
