---
mbox_unit: 1
unit: sentence-poem
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  sentence-poem-main: 1
  text-input: 3
  ollama: 3
  display: 3
  text-input-api: 2
  ollama-api: 3
  display-api: 2
---

# sentence-poem

An interactive poem maker: ask the user for a sentence, ask Ollama for a poem of around 100 words inspired by it, and render the poem on the display.

## Definition

```yaml
entryBox: sentence-poem-main
boxes:
  - sentence-poem-main
  - text-input
  - ollama
  - display
bindings:
  - consumer: sentence-poem-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: sentence-poem-main
    interface: ollama-api
    operations: [generate]
    provider: ollama
  - consumer: sentence-poem-main
    interface: display-api
    operations: [show-window, show-string, use-multitext]
    provider: display
externalProviders: []
exposes: []
configuration:
  ollama.baseUrl: "http://localhost:11434"
  ollama.model: "llama3:latest"
```

## Purpose

Composes text entry, Ollama generation, and multiline display into a small poem-writing application.

## Startup behavior

The framework dispatches `run` to `sentence-poem-main` after initialization. The entry box shows the display, asks for a sentence, displays the generated poem, and prompts for another sentence while leaving the most recent poem visible.

## Failure behavior

Cancelling the prompt or submitting empty input terminates cleanly. A generation failure is displayed and the app continues to accept another sentence. Other operation failures propagate through runtime exception handling.

## Test expectations

Against a reachable configured Ollama endpoint, submitting a sentence results in a visible multiline poem of approximately 100 words based on that sentence. Cancellation terminates cleanly, and generation failure produces visible feedback.
