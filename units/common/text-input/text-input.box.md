---
mbox_unit: 1
unit: text-input
type: box
version: 3
uses:
  box: 5
  text-input-api: 2
---

# text-input

Displays a modal text-editing dialog and returns text supplied by the user.

## Definition

```yaml
provides:
  - interface: text-input-api
    operations: [prompt]
consumes: []
configuration: {}
sideEffects:
  - Displays a modal dialog on the local desktop and accepts keyboard input.
```

## Responsibility boundary

Owns the modal text-input dialog while it is open, including optional prefilled text and multiline editing. It does not own clipboard access, multi-field forms, or input validation beyond capturing the entered string.

## State assumptions

Stateless between calls. While a `text-input-api.prompt` handler is in progress it owns a transient modal dialog.

## Failure behavior

The single declared operational failure is `input-cancelled`. Underlying windowing failures propagate as exceptions per the framework's default behavior.

## Test expectations

A confirmed dialog returns the entered text. Single-line requests remain compact; multiline requests expose a scrollable editing surface and preserve line breaks. A cancelled or closed dialog reports the `input-cancelled` failure.
