---
mbox_unit: 1
unit: sentence-poem-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  ollama-api: 3
  display-api: 2
---

# sentence-poem-main

The entry box for the `sentence-poem` app. It collects sentences, requests poems from Ollama, and sends them to the display.

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

Owns the poem request loop and prompt construction. User input, model communication, and display rendering belong to the bound providers.

## State assumptions

Stateless across requests. Each submitted sentence initiates one independent generation operation using the configured Ollama endpoint and model.

## Failure behavior

An `input-cancelled` response or an empty sentence requests orderly shutdown. A failed generation response is presented in the display before prompting again. Unexpected operation failures propagate from `run`.

## Startup behavior

The `run` invocation:

1. Shows a display window and presents a waiting message.
2. Prompts the user to enter a sentence.
3. If the user cancels or provides empty text, requests application shutdown.
4. Presents a writing message and sends `ollama-api.generate` a prompt requesting only a poem of around 100 words inspired by the entered sentence.
5. Sends successful poem text, or a generation failure notice, to `display-api.use-multitext`.
6. Repeats from step 2, leaving the last poem visible while awaiting another sentence.

## Test expectations

A submitted sentence produces one poem-generation request and shows its returned text in the display. Cancellation and empty input shut down without generation. A generation failure is displayed and permits another request.
