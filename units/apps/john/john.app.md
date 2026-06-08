---
mbox_unit: 1
unit: john
type: app
version: 12
uses:
  app: 6
  runtime: 1
  web-pixel: 21
  john-main: 11
---

# John App

A web-pixel app that represents one moving bui in the shared world.

## Definition

```yaml
entryBox: john-main
boxes:
  - john-main
bindings: []
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

John demonstrates the web-pixel world model: an app-owned bui with a white center point and connected red exl body pixels.

## Startup Behavior

When john has no saved framework configuration, the framework picks an unoccupied random center location, attaches ten connected exls around it, assigns a random full momentum vector, and grants 100 power points. Each tick, john receives a turn, reads previous-turn power and pill-collection feedback, pays to look when it needs a fresh pill target and has enough surplus power, keeps hunting remaining pills from the same look before paying to look again, steers toward an interception point near visible pills, moves two exls toward the selected target when the framework accepts the body operation, otherwise follows a blind sweeping search pattern, and applies steering power only when momentum is materially off course or speed is low.

## Failure Behavior

If john's center hits world bounds, john keeps its current body position and the framework mirrors its momentum angle. If framework-owned movement is blocked by occupied pixels, exl bounds, or body disconnection, john keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

The framework root page should show john's power and activation button above a black 200 by 150 logical world. When john is active, its white center and red exls should move through the world, remain connected, display a smaller fading look-radius overlay when it spends 4 power to see, use exl body movement to reach visible pills, chain through additional known visible pills after a collection, and fall back to lane sweeping when it has no target.

## Framework Realization

The executable browser realization is stored under `/impl/web-pixel/apps/john`.
