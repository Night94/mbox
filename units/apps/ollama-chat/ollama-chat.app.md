---
mbox_unit: 1
unit: ollama-chat
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  ollama-chat-main: 1
  text-input: 3
  ollama: 3
  display: 3
  text-to-speech: 2
  text-input-api: 2
  ollama-api: 3
  display-api: 2
  text-to-speech-api: 1
---

# ollama-chat

An interactive chat loop: prompt the user, send the input to an Ollama model, then display and speak the response.

## Definition

```yaml
entryBox: ollama-chat-main
boxes:
  - ollama-chat-main
  - text-input
  - ollama
  - display
  - text-to-speech
bindings:
  - consumer: ollama-chat-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: ollama-chat-main
    interface: ollama-api
    operations: [generate]
    provider: ollama
  - consumer: ollama-chat-main
    interface: display-api
    operations: [show-window, show-string, use-multitext]
    provider: display
  - consumer: ollama-chat-main
    interface: text-to-speech-api
    operations: [say-and-wait]
    provider: text-to-speech
externalProviders: []
exposes: []
configuration:
  ollama.baseUrl: "http://localhost:11434"
  ollama.model: "llama3:latest"
  tts.speakerId: 1
  tts.speed: 1.0
```

## Purpose

Composes the four common boxes that make a usable interactive Ollama chat: input, generation, display, voice.

## Startup behavior

The framework dispatches `run` to `ollama-chat-main` after `init` completes. The entry box positions the window, then loops on prompt/generate/display/speak until the user cancels.

## Failure behavior

The user cancelling the prompt is the orderly termination path. Generation failures are displayed and the loop continues. Other operation failures terminate `run` with an exception and trigger application shutdown via the runtime's fatal-`run` handling.

## Test expectations

Against a reachable Ollama endpoint, a confirmed entry produces a displayed and spoken response, then re-prompts. A failed generation shows the failure and re-prompts. Cancelling terminates cleanly.
