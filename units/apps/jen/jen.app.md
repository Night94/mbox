---
mbox_unit: 1
unit: jen
type: app
version: 1
uses:
  app: 6
  runtime: 1
  web-pixel: 21
  jen-main: 1
---

# Jen App

A web-pixel app that represents one moving bui in the shared world.

## Definition

```yaml
entryBox: jen-main
boxes:
  - jen-main
bindings: []
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

Jen demonstrates a paid-look and grab strategy in the web-pixel world: it spends power for local vision, remembers visible pill targets, stretches its exls toward a selected pill, and falls back to sweeping the field when it has no target.

## Startup Behavior

When jen has no saved framework configuration, the framework picks an unoccupied random center location, attaches ten connected exls around it, assigns a random full momentum vector, and grants 100 power points. Each tick, jen receives a turn, reads previous-turn power and pill-collection feedback, prunes collected or stale known pills, pays to look when it needs visible pill information and has enough surplus power, selects a retained visible pill target, moves two exls toward that target when the framework accepts the body operation, steers toward an interception point near the selected pill, and follows a lane-sweeping fallback when it has no target.

## Failure Behavior

If jen's center hits world bounds, jen keeps its current body position and the framework mirrors its momentum angle. If framework-owned movement is blocked by occupied pixels, exl bounds, or body disconnection, jen keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

The framework root page should show jen's power and activation button above a black 200 by 150 logical world. When jen is active, its white center and pink exls should move through the world, remain connected, display a fading look-radius overlay when it spends 4 power to see, use exl body movement to grab toward visible pills, chain through retained visible pills after a collection, and fall back to lane sweeping when it has no target.

## Framework Realization

The executable browser realization is stored under `/impl/web-pixel/apps/jen`.
