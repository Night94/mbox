---
mbox_unit: 1
unit: bday
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  bday-main: 1
  text-input: 3
  text-to-speech: 2
  ollama: 3
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# bday

A one-turn birthday companion: ask for the user's birthday aloud and in a dialog, ask Ollama which famous people share it, then speak the answer.

## Definition

```yaml
entryBox: bday-main
boxes:
  - bday-main
  - text-input
  - text-to-speech
  - ollama
bindings:
  - consumer: bday-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: bday-main
    interface: text-to-speech-api
    operations: [say, say-and-wait]
    provider: text-to-speech
  - consumer: bday-main
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

Composes text input, local speech, and Ollama generation into a short spoken birthday lookup.

## Startup behavior

The framework dispatches `run` to `bday-main` after `init` completes. The entry box opens a birthday input prompt while initiating the spoken question, generates a short answer for a submitted birthday, speaks the answer, and then shuts down.

## Failure behavior

Cancelling the prompt or submitting an empty birthday terminates cleanly. If Ollama reports a generation failure, the app speaks a short failure notice and terminates cleanly. Other operation failures propagate as exceptions through the runtime.

## Test expectations

A confirmed birthday against a reachable Ollama endpoint results in a spoken list of famous people sharing that birthday. The initial prompt is both visible and audible. Cancellation terminates without making a generation request.
