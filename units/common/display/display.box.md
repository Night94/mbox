---
mbox_unit: 1
unit: display
type: box
version: 3
uses:
  box: 5
  display-api: 2
---

# display

A simple windowed display. The window is hidden when the box is created and becomes visible only when `display-api.show-window` is invoked. Text content is set with `display-api.show-string` (single centered line) or `display-api.use-multitext` (scrollable multiline text filling the window and positioned at its latest content).

## Definition

```yaml
provides:
  - interface: display-api
    operations: [show-window, hide-window, show-string, use-multitext]
consumes: []
configuration: {}
sideEffects:
  - Creates, shows, hides, positions, and updates a top-level window on the local desktop.
```

## Responsibility boundary

Owns one windowed display surface and its current content. It does not own input focus, multi-window composition, or display content sourced from other boxes beyond what is sent via its provided messages.

## State assumptions

Holds the window handle, its current visibility, its current geometry, and its current content (either a centered single line or a multiline text). All state is owned by the box and is not shared with other boxes.

## Failure behavior

Operational failures are limited to `display-api.show-window`'s `unknown-monitor`. Failures originating in the underlying windowing system propagate as exceptions per the framework's default behavior.

## Test expectations

After creation, the window is not visible. A successful `display-api.show-window` makes it visible on the requested monitor with the requested fractional geometry. `display-api.show-string` and `display-api.use-multitext` each replace the window content with the expected rendering, and multiline replacement content is scrolled to its end. `display-api.hide-window` removes the window from view while preserving its content for the next show.
