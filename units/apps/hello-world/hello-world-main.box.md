---
mbox_unit: 1
unit: hello-world-main
type: box
version: 2
uses:
  box: 5
  display-api: 2
---

# hello-world-main

The entry box for the `hello-world` app. Waits, asks the display to show a greeting, waits again, then requests shutdown.

## Definition

```yaml
provides: []
consumes:
  - interface: display-api
    operations: [show-window, show-string]
configuration: {}
sideEffects: []
```

## Responsibility boundary

Owns only its own driver sequence. It does not own how the greeting is rendered (the `display` provider owns that) or the application shutdown mechanism (the framework owns that).

## State assumptions

Stateless across invocations. Its `run` invocation executes a single fixed sequence and returns.

## Failure behavior

A failure of the consumed `display-api.show-string` operation propagates as an exception from `run`. No declared operational failures.

## Startup behavior

The `run` invocation:

1. Sends `display-api.show-window` to show a centered display window on monitor 0.
2. Sleeps for 5 seconds.
3. Sends `display-api.show-string` with `text: "hello world"` and waits for the response.
4. Sleeps for 5 seconds.
5. Requests application shutdown.

## Test expectations

A run against a real `display-api` provider results in the string `hello world` being shown on the display approximately five seconds after startup, and the application terminating approximately five seconds after that.
