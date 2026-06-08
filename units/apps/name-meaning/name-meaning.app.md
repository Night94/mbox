---
mbox_unit: 1
unit: name-meaning
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  name-meaning-main: 1
  text-input: 3
  text-to-speech: 2
  ollama: 3
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# name-meaning

A one-turn spoken name guide: ask for the user's first and last name, ask Ollama about their likely origins and meanings, then speak a concise response.

## Definition

```yaml
entryBox: name-meaning-main
boxes:
  - name-meaning-main
  - text-input
  - text-to-speech
  - ollama
bindings:
  - consumer: name-meaning-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: name-meaning-main
    interface: text-to-speech-api
    operations: [say, say-and-wait]
    provider: text-to-speech
  - consumer: name-meaning-main
    interface: ollama-api
    operations: [generate]
    provider: ollama
externalProviders: []
exposes: []
configuration:
  ollama.baseUrl: "http://localhost:11434"
  ollama.model: "llama3:latest"
  tts.speakerId: 1
  tts.speed: 1.0
```

## Purpose

Composes text input, Ollama generation, and local speech into a short exploration of a submitted full name.

## Startup behavior

The framework dispatches `run` to `name-meaning-main` after initialization. The entry box asks for the user's first and last name, requests an etymology summary from Ollama, limits the spoken answer to fewer than 100 words, speaks it, and shuts down.

## Failure behavior

Cancelling the prompt or submitting an empty name terminates cleanly. If Ollama reports a generation failure, the app speaks a short failure notice and terminates cleanly. Other operation failures propagate as exceptions through the runtime.

## Test expectations

A confirmed full name against a reachable Ollama endpoint results in a spoken response discussing the likely origin and meaning of both names in fewer than 100 words. Cancellation terminates without making a generation request.
