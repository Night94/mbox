---
mbox_unit: 1
unit: bday-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# bday-main

The entry box for the `bday` app. It collects one birthday, requests famous birthday matches from Ollama, and speaks the result.

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

Owns the one-turn dialog and response sequence. Capturing text, speech playback, and model communication belong to the bound providers.

## State assumptions

Stateless. The Ollama endpoint and model are read from configuration during `run`.

## Failure behavior

An `input-cancelled` response or empty birthday triggers orderly shutdown. A non-success generation response is converted into a short spoken notice. Unexpected operation failures propagate from `run`.

## Startup behavior

The `run` invocation:

1. Initiates `text-input-api.prompt` for a birthday and `text-to-speech-api.say` with `What's your birthday?` concurrently.
2. If no birthday is supplied, requests shutdown.
3. Sends `ollama-api.generate` with a prompt requesting a concise, spoken-friendly list of famous people sharing the submitted birthday.
4. Sends `text-to-speech-api.say-and-wait` with either the generated answer or a lookup failure notice.
5. Requests application shutdown.

## Test expectations

Submitting a birthday initiates one generation request and speaks its successful response. Cancelling or submitting empty input terminates without calling Ollama. A generation failure produces the spoken fallback notice.
