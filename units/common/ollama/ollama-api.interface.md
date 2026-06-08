---
mbox_unit: 1
unit: ollama-api
type: interface
version: 3
uses:
  interface: 1
  schema: 2
---

# ollama-api

Provides non-streaming text generation through an Ollama HTTP endpoint.

## Definition

```yaml
operations:
  generate:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        baseUrl: { type: string, minLength: 1, maxLength: 2048 }
        model: { type: string, minLength: 1, maxLength: 255 }
        prompt: { type: string, minLength: 0 }
        format: { type: any }
        temperature: { type: number }
      required: [baseUrl, model, prompt]
      additionalProperties: false
    response:
      type: object
      properties:
        model: { type: string }
        response: { type: string }
      required: [model, response]
      additionalProperties: false
    failures:
      invalid-base-url: "`baseUrl` is not an absolute HTTP or HTTPS URL."
      ollama-http-error: Ollama returned a non-success HTTP status.
    behavior:
      - Sends a non-streaming `POST <baseUrl>/api/generate` request with `model` and `prompt`.
      - Supplies optional `format` as Ollama's structured output format and optional `temperature` as the generation temperature.
      - Returns `model` and generated `response` once generation completes.
```

## Compatibility rules

Changing endpoint, model, structured-format, generation-option, response, or failure semantics requires reevaluation of providers and consumers.

Providers may log diagnostic HTTP status and remote error details when reporting `ollama-http-error`; those details are not part of the declared failure response.

## Test expectations

A valid reachable endpoint returns generated text; a structured `format` request is passed to Ollama for constrained generation; invalid URLs and unsuccessful HTTP responses produce the declared failures.
