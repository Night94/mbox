---
mbox_unit: 1
unit: worker-api
type: interface
version: 1
uses:
  interface: 1
  schema: 2
---

# worker-api

Provides small arithmetic operations used to exercise request handling, failures, delay, and concurrency.

## Definition

```yaml
operations:
  add:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        a: { type: integer }
        b: { type: integer }
      required: [a, b]
      additionalProperties: false
    response:
      type: object
      properties:
        sum: { type: integer }
      required: [sum]
      additionalProperties: false
    failures: {}
    behavior:
      - Returns the integer sum of the two input values.
  slow-add:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        a: { type: integer }
        b: { type: integer }
        delayMs: { type: integer, minimum: 0 }
      required: [a, b, delayMs]
      additionalProperties: false
    response:
      type: object
      properties:
        sum: { type: integer }
      required: [sum]
      additionalProperties: false
    failures: {}
    behavior:
      - Waits at least `delayMs` milliseconds before responding.
      - Returns the integer sum of the two input values.
      - Concurrent invocations on one provider must not block each other.
  divide:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        a: { type: integer }
        b: { type: integer }
      required: [a, b]
      additionalProperties: false
    response:
      type: object
      properties:
        quotient: { type: number }
      required: [quotient]
      additionalProperties: false
    failures:
      divide-by-zero: The divisor `b` is zero.
    behavior:
      - Returns `a / b` as a real-valued number.
      - Reports `divide-by-zero` as a declared failure when `b` is zero.
```

## Compatibility rules

Changing arithmetic field semantics, the delay or parallelism guarantee, or the divide failure outcome requires reevaluation of providers and consumers of the affected operation.

## Test expectations

Providers return correct arithmetic results, honor the `slow-add` delay without serializing overlapping calls, and report `divide-by-zero` for division by zero.
