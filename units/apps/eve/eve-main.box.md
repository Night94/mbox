---
mbox_unit: 1
unit: eve-main
type: box
version: 6
uses:
  box: 5
---

# Eve Main Box

The entry box for the eve app.

## Definition

```yaml
provides: []
consumes: []
configuration: {}
sideEffects:
  - Moves a bui with a white center and blue exls through the browser-hosted web-pixel world.
```

## Purpose

This box owns the app behavior for a single bui named eve.

## Responsibility Boundary

The box receives a framework tick, observes its bui state, applies tiny aligned survival pulses when speed is low, and requests two-exl body operations to maintain a connected blind sweeping net. The web-pixel framework owns the world matrix, momentum, power, movement penalties, pills, movement update, body-operation validation, collision checks, bui center and exl persistence, activation state, debug logging, and rendering.

## State Assumptions

The box stores its heading, sweep phase, last survival pulse, and last body-move plan/result for debugging. The framework stores eve's center, exl offsets, momentum vector, power, movement-cost count, swallowed-pill count, and fractional motion carry in its world configuration record.

## Failure Behavior

If eve's center hits world bounds, the framework mirrors its momentum angle. If framework movement is blocked by occupied pixels, exl bounds, or body disconnection, eve keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

Opening the web-pixel framework should show eve as a connected body with a white center and blue exls. When active, eve should conserve power by coasting on framework-owned momentum, using tiny survival pulses only when speed is low, and using two-exl body operations to sweep blindly.
