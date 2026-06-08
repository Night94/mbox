---
mbox_unit: 1
unit: web-pixel
type: framework
version: 21
uses:
  framework: 2
  runtime: 1
---

# Web Pixel Framework

A static browser-hosted framework for MBOX apps that live as buis inside a shared pixel world.

## Definition

```yaml
implements:
  - runtime: 1
host: browser-javascript
processTopology: single-process
boxImplementationDiscovery:
  convention: per-framework-subdirectory
  frameworkSubdirectory: web-pixel
configurationSources:
  - application.json
```

## Purpose

The web-pixel framework runs MBOX applications as static HTML and JavaScript in a browser. Its root page hosts a single world view and represents registered apps with activation buttons.

## Scope

The framework provides a 200 by 150 logical pixel matrix with a black default background. Each logical pixel is rendered as a 5 by 4 browser-pixel rectangle. Apps live inside the matrix as buis: each bui has one white center pixel and zero or more colored exls, which are body pixels stored as relative offsets from the center. Each app may declare the display color used for its exls in the framework app manifest. The framework UI can reset the world, clearing all pills and recreating every app with one random center and ten connected random exls.

Each active app receives one tick callback per framework tick. Ticks normally occur every 90 milliseconds. During the app turn it may observe its own bui state, current power, current movement-cost count, previous-turn power delta, and previous-turn pills collected, may apply one power vector, may request one body operation that moves exactly two exls, and may pay to look. Movement is owned by the framework. The framework UI shows each app's power above its activation button. If a bui reaches zero power, the framework deactivates it.

The framework UI provides a turbo toggle. Turbo mode schedules ticks with no intentional delay between ticks. Turbo mode automatically turns off when one or zero buis remain with power.

Each bui has a framework-stored two-dimensional momentum vector. Each vector component is between -1 and 1, and the vector magnitude is capped at 1. A full momentum vector moves the center one logical pixel per turn. Fractional components accumulate across turns, so momentum of 0.5 on the x axis advances that axis every other turn. Diagonal movement is represented by non-zero x and y components. After each successful move, momentum decays to 99% of its prior value.

During its turn, an app may apply one power vector. The power vector has magnitude capped at 1, costs power points proportional to its magnitude, and changes the bui's momentum. The app controls thrust by choosing the direction and magnitude of that vector. A larger applied power vector changes direction or speed faster than a smaller one. Power can accelerate, decelerate, or steer the bui.

During its turn, an app may look once when it has at least 4 power points. Looking costs 4 power points and returns all visible world pixels within Euclidean distance 40 of the bui center, including pills and bui body pixels. The browser realization draws a temporary circular overlay around the look radius for one second and fades it out so the look event is visible on screen.

During its turn, an app may request one two-exl body operation. Each requested exl move identifies an exl by its current array index and one of the eight non-zero neighbor directions. The framework evaluates both requested exl moves as one final body shape. If the resulting center and exls remain in bounds, avoid occupied pixels, avoid duplicates, and keep every exl connected to the center or another exl by an edge or corner, the framework commits both exl moves and checks for pill collection. If either requested exl move or the final body shape is invalid, the call is ignored and no exl is moved.

After each active app's turn, the framework advances that app's bui according to its momentum. Movement shifts the bui forward when the target center and translated exls are in bounds and not occupied by another bui. On each successful forward movement, the framework increments the bui's movement-cost count; every tenth successful movement costs one power point and resets the counted block. This movement cost applies whether or not the app also used power during the turn. On each successful forward movement, a random half of the exls are considered for lagging; each considered exl has a 50% chance to remain at its previous absolute location while the rest of the bui advances, but only when accepting that lag keeps the whole body in bounds, unoccupied, duplicate-free, and connected. If the center or any exl attempts to leave the world bounds, the bui stays in place for that turn and the framework reflects the momentum at the mirrored angle by reversing the out-of-bounds axis while preserving the current speed. If movement crashes into another bui or body disconnection, the bui remains in place and the framework assigns a random full momentum vector.

When an app has no saved world configuration, the framework chooses an unoccupied random center, attaches ten connected exls randomly, assigns a random full momentum vector, grants 100 power points, and starts the movement-cost count at zero. Bui center locations, exl offsets, momentum vectors, power, movement-cost counts, previous-turn feedback, swallowed-pill count, and fractional motion carry are stored by the framework after each turn. In the static browser realization, that writable framework configuration is persisted in browser local storage; app-local state is stored separately by each app under its own namespace.

The framework can add pills to the world. Pills are yellow world pixels that do not block movement. When an exl reaches a pill, the pill is removed and that bui gains 10 power points. The framework tracks swallowed pills for each bui; for every ten pills swallowed, the bui gains one connected exl when a valid adjacent location is available. Power is spent by applying power vectors, looking, and repeated successful movement. A bui with no power is deactivated by the framework until it is reset.

The browser realization records recent body-operation, power-application, look, movement-penalty, and failed movement debug events in local storage and exposes them through `WebPixelWorld.getDebugLog()` and `WebPixelWorld.clearDebugLog()`. Each logged body operation records the tick, app, requested exl moves, result, and rejection details when available. Power events record the requested vector and momentum before and after application. Look events record the cost, radius, visible pixel count, visible pill count, and remaining power. Movement-penalty events record the charged amount, remaining power, and retained movement-cost count. Failed movement events record the requested step, result reason, and momentum before and after framework handling.

## Host Environment Requirements

The framework requires a modern browser with standard DOM, CSS, local storage, and JavaScript support. It is designed to work when opened from the local filesystem, so app discovery is recorded in a checked-in manifest rather than discovered dynamically from directory listings.

## Known Limitations

This version does not implement inter-box message routing, dynamic box instance creation, schema validation, or the full MBOX lifecycle dispatch contract. It is suitable for single-page bui world simulations while the fuller browser runtime contract is designed.
