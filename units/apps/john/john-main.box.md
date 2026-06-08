---
mbox_unit: 1
unit: john-main
type: box
version: 11
uses:
  box: 5
---

# John Main Box

The entry box for the john app.

## Definition

```yaml
provides: []
consumes: []
configuration: {}
sideEffects:
  - Moves a bui with a white center and red exls through the browser-hosted web-pixel world.
```

## Purpose

This box owns the app behavior for a single bui named john.

## Responsibility Boundary

The box receives a framework tick, observes its own bui state and previous-turn pill feedback, pays the framework look cost when it needs a fresh visible pill target, retains the visible pills from that look, promotes another known pill after a collection or stale target, requests two-exl body operations to stretch connected exls toward the active target, and selectively applies a power vector to steer momentum toward pill interception points or through a blind field-sweeping fallback. The web-pixel framework owns the world matrix, momentum, power, look visibility, movement penalties, pill feedback, pills, movement update, collision checks, bui center and exl persistence, activation state, and rendering.

## State Assumptions

The box stores app-local steering preference, look cooldown, known visible pills, target choice, target age, last body-move decision, and last steering decision. Known visible pills remain available until john collects, reaches, or ages out that target, so one paid look can drive several hunting attempts. The framework stores john's center, exl offsets, momentum vector, power, movement-cost count, previous-turn feedback, swallowed-pill count, and fractional motion carry in its world configuration record.

## Failure Behavior

If john's center hits world bounds, the framework mirrors its momentum angle. If framework movement is blocked by occupied pixels, exl bounds, or body disconnection, john keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

Opening the web-pixel framework should show john as a connected body with a white center and red exls. When active, john should move according to its framework-owned momentum, occasionally draw a fading look-radius overlay, steer toward visible pills, move exls toward the selected pill when valid, promote another known pill after pill feedback, and use the blind search pattern when no pill target is known.
