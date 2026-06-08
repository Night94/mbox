---
mbox_unit: 1
unit: worker-demo
type: app
version: 1
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  worker-demo-main: 1
  worker: 2
  worker-api: 1
---

# worker-demo

Exercises framework concurrency, timeout, declared failure, and explicit instance lifecycle through the `worker` box.

## Definition

```yaml
entryBox: worker-demo-main
boxes:
  - worker-demo-main
  - worker
bindings:
  - consumer: worker-demo-main
    interface: worker-api
    operations: [add, slow-add, divide]
    provider: worker
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

A self-contained smoke test for framework features that are otherwise hard to observe: parallel handler execution per box, per-call timeouts, declared failures, and explicit `createBox`/`destroy` lifecycle.

## Startup behavior

The framework dispatches `run` to `worker-demo-main` after `init` completes. The entry box exercises the framework against the `worker` provider and then requests shutdown.

## Failure behavior

The demonstration provokes a timeout and a declared failure as part of normal operation. Any other operation failure terminates `run` with an exception and triggers application shutdown via the runtime's fatal-`run` handling.

## Test expectations

Running the app produces log entries consistent with the entry box's scripted expectations and terminates cleanly.
