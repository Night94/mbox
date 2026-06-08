---
mbox_unit: 1
unit: tim-main
type: box
version: 11
uses:
  box: 5
---

# Tim Main Box

The entry box for the tim app.

## Definition

```yaml
provides: []
consumes: []
configuration: {}
sideEffects:
  - Moves a bui with a white center and green exls through the browser-hosted web-pixel world.
```

## Purpose

This box owns the app behavior for a single bui named tim.

## Responsibility Boundary

The box receives a framework tick, observes its bui state, applies an aligned power vector only when speed drops below target, and searches for a valid two-exl body operation that improves a perpendicular sweeping line across its movement direction. The web-pixel framework owns the world matrix, momentum, power, movement penalties, pills, movement update, body-operation validation, collision checks, bui center and exl persistence, activation state, debug logging, and rendering.

## State Assumptions

The box stores its last thrust, movement-cost count observation, and body-move plan/result for debugging. The framework stores tim's center, exl offsets, momentum vector, power, movement-cost count, swallowed-pill count, and fractional motion carry in its world configuration record.

## Failure Behavior

If tim's center hits world bounds, the framework mirrors its momentum angle. If framework movement is blocked by occupied pixels, exl bounds, or body disconnection, tim keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

Opening the web-pixel framework should show tim as a connected body with a white center and green exls. When active, tim should move according to its framework-owned momentum while using conservative aligned power vectors and two-exl body operations to maintain a perpendicular pill-sweeping body.
