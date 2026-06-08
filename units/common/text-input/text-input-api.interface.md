---
mbox_unit: 1
unit: text-input-api
type: interface
version: 2
uses:
  interface: 1
  schema: 2
---

# text-input-api

Obtains editable single-line or multiline text from the user.

## Definition

```yaml
operations:
  prompt:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        title: { type: string }
        prompt: { type: string }
        initialText: { type: string }
        multiline: { type: boolean }
      required: [title, prompt]
      additionalProperties: false
    response:
      type: object
      properties:
        text: { type: string }
      required: [text]
      additionalProperties: false
    failures:
      input-cancelled: The user closed the input surface or cancelled the request.
    behavior:
      - Presents an input prompt identified by `title` and `prompt`.
      - Uses a multiline editing surface when optional `multiline` is `true`; otherwise uses a single-line editing surface.
      - Prefills the editing surface with optional `initialText`, or an empty string when it is absent.
      - Returns entered `text` when the user confirms.
      - Reports `input-cancelled` when the user cancels.
```

## Compatibility rules

Changing entry modes, prefill behavior, or confirm/cancel semantics requires reevaluation of providers and consumers.

## Test expectations

A confirmed single-line or multiline prompt returns the entered text, including line breaks; supplied initial text is shown for editing; a cancelled prompt reports `input-cancelled`.
