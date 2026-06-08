---
mbox_unit: 1
unit: ollama
type: box
version: 3
uses:
  box: 5
  ollama-api: 3
---

# ollama

Submits plain or schema-constrained text generation requests to a reachable Ollama HTTP API.

## Definition

```yaml
provides:
  - interface: ollama-api
    operations: [generate]
consumes: []
configuration: {}
sideEffects:
  - Issues outbound HTTP requests to the Ollama instance identified per request.
```

## Responsibility boundary

Owns the HTTP exchange with an Ollama instance for one generation request at a time per call, including optional structured-output format and temperature parameters. It does not own model selection policy, prompt construction, response post-processing, or Ollama instance discovery.

## State assumptions

Stateless. Each handler call opens, uses, and releases its own HTTP client resources.

## Failure behavior

The provided message declares `invalid-base-url` and `ollama-http-error`. Connection failures, malformed remote responses, and request timeouts propagate as exceptions per the framework's default behavior. A multi-minute internal timeout applies to a single generation request.

## Test expectations

A reachable Ollama instance returns generated text, including JSON constrained by a supplied format schema when Ollama supports it. A non-absolute URL reports `invalid-base-url`. A non-success HTTP status from Ollama reports `ollama-http-error`.
