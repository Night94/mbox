---
mbox_unit: 1
unit: hello-world
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  hello-world-main: 2
  display: 3
  display-api: 2
---

# hello-world

A minimal demonstration app: after a short delay, it shows `hello world` on the display and then shuts down.

## Definition

```yaml
entryBox: hello-world-main
boxes:
  - hello-world-main
  - display
bindings:
  - consumer: hello-world-main
    interface: display-api
    operations: [show-window, show-string]
    provider: display
externalProviders: []
exposes: []
configuration: {}
```

## Purpose

The smallest interesting MBOX app: one entry box, two consumed display interface operations, one provider, no configuration. Useful as a first-run smoke test and as the simplest worked example of the app composition shape.

## Startup behavior

The framework dispatches `run` to `hello-world-main` after its `init` completes. Per the entry box, the run sequence opens the display window, waits, shows the greeting, waits again, and calls shutdown.

## Failure behavior

A failure of `display-api.show-string` propagates out of `hello-world-main.run` and is logged as a fatal `run` failure by the framework, which triggers application shutdown.

## Test expectations

Launching the app shows `hello world` on the local display roughly five seconds after startup and the process terminates cleanly roughly five seconds after the greeting appears.
