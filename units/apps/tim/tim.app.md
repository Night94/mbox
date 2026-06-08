---
mbox_unit: 1
unit: tim
type: app
version: 11
uses:
  app: 6
  runtime: 1
  web-pixel: 21
  tim-main: 11
---

# Tim App

A web-pixel app that represents one moving bui in the shared world.

## Definition

```yaml
entryBox: tim-main
boxes:
  - tim-main
bindings: []
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

Tim demonstrates the web-pixel world model: an app-owned bui with a white center point and connected green exl body pixels.

## Startup Behavior

When tim has no saved framework configuration, the framework picks an unoccupied random center location, attaches ten connected exls around it, assigns a random full momentum vector, and grants 100 power points. Each tick, tim receives a turn, applies an aligned power vector only when speed is below target, searches for a valid two-exl body move that improves a line perpendicular to its movement direction, and the framework advances tim according to stored momentum.

## Failure Behavior

If tim's center hits world bounds, tim keeps its current body position and the framework mirrors its momentum angle. If framework-owned movement is blocked by occupied pixels, exl bounds, or body disconnection, tim keeps its current body position and the framework assigns a new random full momentum vector.

## Test Expectations

The framework root page should show tim's power and activation button above a black 200 by 150 logical world. When tim is active, its white center and green exls should move in a straight momentum line except for framework bounces or collision handling, remain connected, conserve power while holding useful speed, and reshape toward a perpendicular sweeping body.

## Framework Realization

The executable browser realization is stored under `/impl/web-pixel/apps/tim`.
