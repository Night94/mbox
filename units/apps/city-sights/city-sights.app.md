---
mbox_unit: 1
unit: city-sights
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  city-sights-main: 1
  text-input: 3
  text-to-speech: 2
  ollama: 3
  text-input-api: 2
  text-to-speech-api: 1
  ollama-api: 3
---

# city-sights

A one-turn spoken sightseeing guide: ask for a city name, ask Ollama for the five most important sightseeing sites of that city, then speak only their names.

## Definition

```yaml
entryBox: city-sights-main
boxes:
  - city-sights-main
  - text-input
  - text-to-speech
  - ollama
bindings:
  - consumer: city-sights-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: city-sights-main
    interface: text-to-speech-api
    operations: [say, say-and-wait]
    provider: text-to-speech
  - consumer: city-sights-main
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

Composes text input, Ollama generation, and local speech into a short spoken list of the five most important sightseeing sites of a submitted city.

## Startup behavior

The framework dispatches `run` to `city-sights-main` after initialization. The entry box asks the user to enter a city name, requests the five most important sightseeing sites from Ollama, extracts the site names, speaks them, and shuts down.

## Failure behavior

Cancelling the prompt or submitting an empty city name terminates cleanly. If Ollama reports a generation failure, the app speaks a short failure notice and terminates cleanly. Other operation failures propagate as exceptions through the runtime.

## Test expectations

A confirmed city name against a reachable Ollama endpoint results in five spoken site names. Cancellation terminates without making a generation request.
