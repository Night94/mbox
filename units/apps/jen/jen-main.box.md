---
mbox_unit: 1
unit: jen-main
type: box
version: 1
uses:
  box: 5
---

# Jen Main Box

The entry box for the jen app.

## Definition

```yaml
provides: []
consumes: []
configuration: {}
sideEffects:
  - Moves a bui with a white center and pink exls through the browser-hosted web-pixel world.
```

## Purpose

This box owns the app behavior for a single bui named jen.

## Responsibility Boundary

The box receives a framework tick, observes its own bui state and previous-turn pill feedback, pays the framework look cost when it needs visible pill information, retains visible pills from that look, selects a pill target, requests two-exl body operations to grab toward that target, and applies power vectors to steer either toward the target or through a lane-sweeping fallback. The web-pixel framework owns the world matrix, momentum, power, look visibility, movement penalties, pill feedback, pills, movement update, collision checks, bui center and exl persistence, activation state, debug logging, and rendering.

## State Assumptions

The box stores app-local look cooldown, known visible pills, current target, target age, lane-sweep state, last look result, last body-move decision, and last steering decision. Known visible pills remain available until jen collects them, reaches them, or ages them out, so one paid look can drive several grab attempts. The framework stores jen's center, exl offsets, momentum vector, power, movement-cost count, previous-turn feedback, swallowed-pill count, and fractional motion carry in its world configuration record.

## Failure Behavior

If jen's center hits world bounds, the framework mirrors its momentum angle. If framework movement is blocked by occupied pixels, exl bounds, or body disconnection, jen keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

Opening the web-pixel framework should show jen as a connected body with a white center and pink exls. When active, jen should move according to its framework-owned momentum, occasionally draw a fading look-radius overlay, retain visible pill targets, move exls toward a selected pill when valid, steer toward that target, and use the lane-sweeping fallback when no pill target is known.
