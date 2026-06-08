---
mbox_unit: 1
unit: city-sights-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# city-sights-main

The entry box for the `city-sights` app. It collects one city name, requests the five most important sightseeing sites from Ollama, and speaks only their names.

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

Owns the one-turn prompt, the sightseeing request, and the extraction of site names from the model response. Capturing text, model communication, and speech playback belong to the bound providers.

## State assumptions

Stateless. The Ollama endpoint and model are read from configuration during `run`.

## Failure behavior

An `input-cancelled` response or empty submitted city triggers orderly shutdown. A non-success generation response is converted into a short spoken notice. Unexpected operation failures propagate from `run`.

## Startup behavior

The `run` invocation:

1. Initiates `text-input-api.prompt` for a city name and `text-to-speech-api.say` with the same question concurrently.
2. If no city is supplied, requests shutdown.
3. Sends `ollama-api.generate` with a prompt requesting the five most important sightseeing sites of the submitted city as a plain numbered list of names only.
4. Extracts up to five site names from a successful generated response, or selects a short lookup failure notice.
5. Sends `text-to-speech-api.say-and-wait` with the names joined by sentence breaks.
6. Requests application shutdown.

## Test expectations

Submitting a city name initiates one generation request and speaks five site names. Cancelling or submitting empty input terminates without calling Ollama. A generation failure produces the spoken fallback notice.
