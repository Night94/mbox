---
mbox_unit: 1
unit: text-to-speech-api
type: interface
version: 1
uses:
  interface: 1
  schema: 2
---

# text-to-speech-api

Speaks supplied text either without waiting for completion or with completion acknowledgement.

## Definition

```yaml
operations:
  say:
    kind: command
    expectsResponse: false
    input:
      type: object
      properties:
        text: { type: string }
      required: [text]
      additionalProperties: false
    response: null
    failures: {}
    behavior:
      - Begins playback of `text` through the provider configured voice.
      - Does not deliver a response.
  say-and-wait:
    kind: command
    expectsResponse: true
    input:
      type: object
      properties:
        text: { type: string }
      required: [text]
      additionalProperties: false
    response: null
    failures: {}
    behavior:
      - Plays `text` through the provider configured voice to completion.
      - Responds successfully only after playback has finished.
```

## Compatibility rules

Changing response expectations or completion acknowledgement semantics requires reevaluation of providers and consumers of the affected operation.

## Test expectations

Providers initiate audible playback for `say`; `say-and-wait` completes successfully only after audible playback finishes.
