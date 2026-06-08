---
mbox_unit: 1
unit: speak-main
type: box
version: 1
uses:
  box: 5
  text-input-api: 2
  text-to-speech-api: 1
---

# speak-main

The entry box for the `speak` app. Loops asking the user for text and speaking it back through the local neural voice.

## Definition

```yaml
provides: []
consumes:
  - interface: text-input-api
    operations: [prompt]
  - interface: text-to-speech-api
    operations: [say-and-wait]
configuration: {}
sideEffects: []
```

## Responsibility boundary

Owns only the prompt/speak loop. Rendering and audio output belong to the bound providers.

## State assumptions

Stateless across iterations.

## Failure behavior

A `text-input-api.prompt` response with the declared `input-cancelled` failure terminates the loop and triggers shutdown. Other operation failures propagate as exceptions from `run`.

## Startup behavior

The `run` invocation:

1. Sends `text-input-api.prompt` with prompt `Enter text to speak:`.
2. If the response declares `input-cancelled`, or the returned `text` is empty, requests application shutdown.
3. Otherwise sends `text-to-speech-api.say-and-wait` with the entered text and waits for completion.
4. Repeats from step 1.

## Test expectations

Confirming the dialog with non-empty text produces audible speech of that text before the next prompt appears. Cancelling the dialog or submitting empty text terminates the app cleanly.
