---
mbox_unit: 1
unit: worker
type: box
version: 2
uses:
  box: 5
  worker-api: 1
---

# worker

A small compute box used to exercise the framework. Its handlers are deliberately simple so demo and test apps can focus on framework behavior such as parallel handler execution, request timeouts, and declared failures.

## Definition

```yaml
provides:
  - interface: worker-api
    operations: [add, slow-add, divide]
consumes: []
configuration: {}
sideEffects: []
```

## Responsibility boundary

Implements three independent arithmetic contracts. It owns no shared mutable state and does not coordinate across requests.

## State assumptions

Stateless. Each handler is a pure computation over its input plus, for `worker-api.slow-add`, the passage of time.

## Failure behavior

Only `worker-api.divide` declares an operational failure (`divide-by-zero`). All other inputs accepted by the schemas produce a successful response. Unexpected runtime errors are not anticipated; if they occur they propagate as exceptions per the framework's default behavior.

## Test expectations

- `worker-api.add` returns the integer sum of its inputs.
- `worker-api.slow-add` returns the integer sum after at least `delayMs` milliseconds and accepts overlapping invocations without serializing them.
- `worker-api.divide` returns the real-valued quotient when `b != 0` and reports `divide-by-zero` when `b == 0`.
