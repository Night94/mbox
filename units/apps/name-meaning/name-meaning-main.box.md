---
mbox_unit: 1
unit: name-meaning-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# name-meaning-main

The entry box for the `name-meaning` app. It collects one full name, requests an origin-and-meaning summary from Ollama, and speaks the result.

## Definition

```yaml
provides: []
consumes:
  - interface: text-input-api
    operations: [prompt]
  - interface: text-to-speech-api
    operations: [say, say-and-wait]
  - interface: ollama-api
    operations: [generate]
configuration:
  ollama.baseUrl:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 2048
  ollama.model:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
sideEffects: []
```

## Responsibility boundary

Owns the one-turn prompt, name-summary request, and response length limit. Capturing text, model communication, and speech playback belong to the bound providers.

## State assumptions

Stateless. The Ollama endpoint and model are read from configuration during `run`.

## Failure behavior

An `input-cancelled` response or empty submitted name triggers orderly shutdown. A non-success generation response is converted into a short spoken notice. Unexpected operation failures propagate from `run`.

## Startup behavior

The `run` invocation:

1. Initiates `text-input-api.prompt` for the user's first and last name and `text-to-speech-api.say` with the same question concurrently.
2. If no name is supplied, requests shutdown.
3. Sends `ollama-api.generate` with a prompt requesting a cautious, spoken-friendly summary of likely origins and meanings for both submitted names in under 90 words.
4. Reduces a successful generated response to no more than 99 words if needed, or selects a short lookup failure notice.
5. Sends `text-to-speech-api.say-and-wait` with the response.
6. Requests application shutdown.

## Test expectations

Submitting a full name initiates one generation request and speaks a response containing fewer than 100 words. Cancelling or submitting empty input terminates without calling Ollama. A generation failure produces the spoken fallback notice.
