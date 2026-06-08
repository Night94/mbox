---
mbox_unit: 1
unit: speak
type: app
version: 1
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  speak-main: 1
  text-input: 3
  text-to-speech: 2
  text-input-api: 2
  text-to-speech-api: 1
---

# speak

A minimal prompt-and-speak loop: ask the user for text, speak it through the local neural voice, repeat until cancelled.

## Definition

```yaml
entryBox: speak-main
boxes:
  - speak-main
  - text-input
  - text-to-speech
bindings:
  - consumer: speak-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: speak-main
    interface: text-to-speech-api
    operations: [say-and-wait]
    provider: text-to-speech
externalProviders: []
exposes: []
configuration:
  tts.speakerId: 1
  tts.speed: 1.0
```

## Purpose

Demonstrates a simple interactive loop binding a text-input provider to a text-to-speech provider with no other dependencies.

## Startup behavior

The framework dispatches `run` to `speak-main` after `init` completes. The entry box loops on prompt/speak until the user cancels.

## Failure behavior

The user cancelling the prompt is the orderly termination path. Other operation failures terminate `run` with an exception and trigger application shutdown via the runtime's fatal-`run` handling.

## Test expectations

Confirming a non-empty entry speaks that entry, then re-prompts. Cancelling or submitting empty text terminates the application cleanly.
