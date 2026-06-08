---
mbox_unit: 1
unit: ollama-chat-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  ollama-api: 3
  display-api: 2
  text-to-speech-api: 1
---

# ollama-chat-main

The entry box for the `ollama-chat` app. Loops prompting the user for text, sending it to an Ollama model, then displaying and speaking the generated response.

## Definition

```yaml
provides: []
consumes:
  - interface: text-input-api
    operations: [prompt]
  - interface: ollama-api
    operations: [generate]
  - interface: display-api
    operations: [show-window, show-string, use-multitext]
  - interface: text-to-speech-api
    operations: [say-and-wait]
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

Owns the chat loop and the per-turn UI sequence. Generation, display rendering, audio playback, and user input belong to the bound providers.

## State assumptions

Stateless across turns. Each turn reads its Ollama endpoint and model from configuration and runs an independent prompt-generate-display-speak sequence.

## Failure behavior

`text-input-api.prompt` declaring `input-cancelled`, or returning empty text, is the orderly termination path and triggers shutdown. An `ollama-api.generate` failure is shown to the user through `display-api.use-multitext` and does not terminate the loop. Other operation failures propagate as exceptions from `run`.

## Startup behavior

The `run` invocation:

1. Sends `display-api.show-window` to position a window on monitor 0 covering the centered half of the working area.
2. Sends `display-api.show-string` with text `Waiting for input...`.
3. Sends `text-input-api.prompt` with prompt `Ask Ollama:`.
4. If the response declares `input-cancelled`, or the returned `text` is empty, requests application shutdown.
5. Sends `display-api.show-string` with text `Generating...`.
6. Sends `ollama-api.generate` with the configured `ollama.baseUrl` and `ollama.model` plus the entered text as `prompt`.
7. On success, sends `display-api.use-multitext` with the returned response text, then `text-to-speech-api.say-and-wait` with that text. On failure, sends `display-api.use-multitext` with the failure text.
8. Repeats from step 3.

## Test expectations

A reachable Ollama endpoint and a confirmed prompt produce displayed and spoken generated text, then re-prompt. A failed Ollama generation shows the failure on the display and re-prompts without terminating. Cancelling or submitting empty text terminates the application cleanly.
