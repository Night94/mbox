---
mbox_unit: 1
unit: text-to-speech
type: box
version: 2
uses:
  box: 5
  schema: 2
  text-to-speech-api: 1
---

# text-to-speech

Speaks submitted text through a local neural voice model, either fire-and-forget or with completion acknowledgement.

## Definition

```yaml
provides:
  - interface: text-to-speech-api
    operations: [say, say-and-wait]
consumes: []
configuration:
  tts.speakerId:
    required: false
    schema:
      type: integer
      minimum: 0
  tts.speed:
    required: false
    schema:
      type: number
      minimum: 0.1
sideEffects:
  - Plays synthesized audio through the local default audio output.
  - Loads a neural speech model into memory for the lifetime of the box.
```

## Responsibility boundary

Owns the loaded speech model and the speech playback pipeline. It does not own audio output device selection, mixing with other audio sources, or text preprocessing beyond what the model performs.

## State assumptions

Holds a loaded neural model and the configured speaker and speed values from creation onward. The model is released when the box is destroyed.

## Failure behavior

Neither provided message declares an operational failure. Model load failures during initialization and playback failures propagate as exceptions per the framework's default behavior.

## Test expectations

After creation, `text-to-speech-api.say` initiates audible playback and does not deliver a response. `text-to-speech-api.say-and-wait` initiates audible playback and responds successfully only after playback completes. Both honor the configured speaker and speed when present.
