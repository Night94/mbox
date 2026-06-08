---
mbox_unit: 1
unit: display-api
type: interface
version: 2
uses:
  interface: 1
  schema: 2
---

# display-api

Controls one windowed display surface and its rendered text content.

## Definition

```yaml
operations:
  show-window:
    kind: command
    expectsResponse: true
    input:
      type: object
      properties:
        monitorId: { type: integer, minimum: 0 }
        width: { type: number, minimum: 0, maximum: 100 }
        height: { type: number, minimum: 0, maximum: 100 }
        left: { type: number, minimum: 0, maximum: 100 }
        top: { type: number, minimum: 0, maximum: 100 }
      required: [monitorId, width, height, left, top]
      additionalProperties: false
    response: null
    failures:
      unknown-monitor: "`monitorId` does not identify an attached monitor."
    behavior:
      - "`width`, `height`, `left`, and `top` are percentages of the chosen monitor working area."
      - If the window is hidden it is made visible.
      - Window content is retained from prior content operations.
  hide-window:
    kind: command
    expectsResponse: true
    input:
      type: object
      properties: {}
      required: []
      additionalProperties: false
    response: null
    failures: {}
    behavior:
      - Hides the window without discarding its content.
  show-string:
    kind: command
    expectsResponse: true
    input:
      type: object
      properties:
        text: { type: string, minLength: 0, maxLength: 100 }
      required: [text]
      additionalProperties: false
    response: null
    failures: {}
    behavior:
      - Replaces prior content and renders `text` as a single centered line.
  use-multitext:
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
      - Replaces prior content and renders `text` in a read-only scrollable multiline control.
      - Positions the multiline view at the end of `text` after each replacement.
```

## Compatibility rules

Changing geometry semantics, rendering behavior, content-retention behavior, or operation failure outcomes requires reevaluation of providers and consumers of the affected operation.

## Test expectations

Providers show and hide the surface as directed, replace content according to the selected rendering operation, keep replacement multiline text scrolled to its end, and report `unknown-monitor` for unavailable monitors.
