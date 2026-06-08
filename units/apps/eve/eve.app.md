---
mbox_unit: 1
unit: eve
type: app
version: 6
uses:
  app: 6
  runtime: 1
  web-pixel: 21
  eve-main: 6
---

# Eve App

A web-pixel app that represents one moving bui in the shared world.

## Definition

```yaml
entryBox: eve-main
boxes:
  - eve-main
bindings: []
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

Eve is a survival-focused competitor: an app-owned bui with a white center point and connected blue exl body pixels.

## Startup Behavior

When eve has no saved framework configuration, the framework picks an unoccupied random center location, attaches ten connected exls around it, assigns a random full momentum vector, and grants 100 power points. Each tick, eve receives a turn, spends tiny aligned survival pulses only when speed is low, reshapes two exls into a blind sweeping net when the framework accepts the move, and the framework advances eve according to stored momentum.

## Failure Behavior

If eve's center hits world bounds, eve keeps its current body position and the framework mirrors its momentum angle. If framework-owned movement is blocked by occupied pixels, exl bounds, or body disconnection, eve keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

The framework root page should show eve's power and activation button above a black 200 by 150 logical world. When eve is active, its white center and blue exls should conserve power through tiny survival pulses only when needed for speed, move by existing momentum, and reshape as a connected blind net.

## Framework Realization

The executable browser realization is stored under `/impl/web-pixel/apps/eve`.
