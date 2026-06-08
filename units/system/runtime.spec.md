---
mbox_unit: 1
unit: runtime
type: spec
version: 1
uses:
  spec: 1
  schema: 2
---

# Runtime Spec

Runtime is a supporting spec that defines the framework guarantees an MBOX application depends on at execution time.

This spec does not introduce units of type `runtime`. A unit depends on this spec when its behavior or interpretation relies on the framework guarantees defined here.

Apps depend on this spec to record which version of the runtime contract they require. A framework unit depends on this spec to record which runtime version(s) it implements.

This spec describes the framework's externally observable behavior and the keys it accepts as application-level tunables. It does not prescribe host-language API syntax, concurrency primitives, transport encodings, or implementation source layout. Those belong to concrete `framework` units.

## Box lifecycle

Every box has a framework-private state, one of:

- `INIT` — the framework has dispatched the box's `init` invocation and its handler has not yet completed.
- `RUNNING` — initialization has completed; ordinary handlers may run.
- `DEINIT` — destruction is in progress; the dispatcher does not start further ordinary handlers; the `deinit` handler runs during this state.
- `ERROR` — a fatal condition has made the box unusable for ordinary work; the box remains addressable for rejection and remains eligible for destruction.

State transitions:

- A new box enters `INIT` when `init` is dispatched.
- A box transitions from `INIT` to `RUNNING` when `init` completes successfully.
- A box transitions to `DEINIT` as soon as destruction is requested.
- A fatal failure transitions a box from `INIT` or `RUNNING` to `ERROR`.
- A destruction request transitions an `ERROR` box to `DEINIT`.

`init`, `run`, and `deinit` are framework lifecycle invocations on a box. They are not interface operations and are not declared in a box's `provides` list.

- Every box receives `init` once at creation and `deinit` once during destruction.
- The app's entry box additionally receives `run` exactly once after `init` completes.
- `init` must complete before the dispatcher starts any ordinary handler.
- `run` may send messages, wait for responses, and request shutdown.
- `deinit` is the last application code invoked in the box during orderly destruction.

## Message dispatch

Every box has one dispatcher that services its inbound queue continuously while the box exists.

- For each `REQ` or `ONCE` received while the box is `RUNNING`, the dispatcher starts an independent handler execution.
- Ordinary handler executions for the same box may run in parallel. The dispatcher remains available to service further messages while existing handlers run or wait.
- Delivery order does not impose handler completion or response order. A box that requires ordering must enforce it in its own logic.
- A `RESP` is matched to the handler execution waiting for the corresponding request id. It allows that execution to continue. It does not invoke a new handler.

Rejections:

- While the box is in `DEINIT`, queued and newly received `REQ` messages are rejected with an `exception` response whose text is `box-deinitializing`. `ONCE` messages are rejected and recorded as warning log entries.
- While the box is in `ERROR`, queued and newly received `REQ` messages are rejected with an `exception` response whose text is `box-error`. `ONCE` messages are rejected and recorded as warning log entries.
- Pending `RESP` messages needed by already-running handlers continue to be matched while the box is in `DEINIT` or `ERROR`, until destruction begins.

A box implementation must protect shared mutable state that may be accessed by parallel handlers. The framework does not serialize ordinary handlers and does not synchronize application state.

## Message methods

Three methods are defined:

- `REQ` — requests work and requires the receiver to send exactly one `RESP`. The sending execution waits until that `RESP` arrives or its request timeout expires.
- `RESP` — sent in response to a received `REQ`. It reuses the receiver, name, and id of its `REQ`. It is matched to the waiting sender by id and resumes that execution.
- `ONCE` — initiates work without a response and does not wait. `ONCE` does not accept a timeout.

The sender assigns a new id to each originated `REQ` or `ONCE`. The receiver of a `REQ` uses the same id when sending its `RESP`.

Every `RESP` payload contains at least:

```yaml
status: <ok | error | exception>
text: <string | null>
```

- `ok` — the operation completed successfully. `text` is `null`. A message that defines a non-null response schema also includes a `result` value conforming to that schema.
- `error` — the operation could not be completed for a declared operational reason. `text` is a failure identifier defined by the operation's `failures` map.
- `exception` — an unexpected remote failure occurred. `text` carries diagnostic information. `result` is not included.

## Box addressing

A running box is addressed as `<box-name>|<instance-id>`. Instance ids are non-negative integers rendered in decimal with no leading zeros.

- Instance id `0` is reserved for a framework-owned default instance, created on first reference.
- Explicitly created instances are assigned positive ids starting at `1`.
- When a message targets a box without specifying an instance id, instance `0` is used.
- Sender and receiver fields in routed messages always contain complete canonical addresses.

## Framework functions

The framework exposes the following functions to box code. The runtime spec defines their observable behavior. Concrete syntax is host-language-specific and belongs to a `framework` unit.

- `sendMsg(REQ, address, name, payload, timeout?) -> response-payload` — initiates a `REQ`, waits for its `RESP`, and returns its payload. If `timeout` is omitted, `runtime.defaultMsgTimeoutMs` applies. If no matching `RESP` arrives before the timeout, the waiting execution is resumed with a local timeout exception. A late-arriving `RESP` is discarded.
- `sendMsg(ONCE, address, name, payload) -> void` — initiates a `ONCE`. Does not wait. Does not accept a timeout.
- `createBox(name) -> instance-id` — creates an instance of the named box. The caller becomes its owner. Returns the new positive instance id. Throws if the box cannot be resolved or instantiated.
- `destroy(address) -> void` — requests destruction of an instance owned by the caller. The target enters `DEINIT` immediately. Throws if the target does not exist or is not owned by the caller. Framework-owned default instances cannot be destroyed by box code.
- `getConfigItem(key) -> structured value` — returns the value associated with `key` in the application's configuration. Throws if `key` is not present. Returns the value with its declared structured type preserved.
- `isCancelled() -> boolean` — returns whether destruction has been requested for the current box. Cancellation is cooperative; the framework does not inject exceptions into running handlers.
- `shutdown()` — requests orderly application shutdown. May be called from any handler. Subsequent calls during shutdown have no additional effect.

A `REQ` may be sent to any box. A `RESP` may always be sent back to the requester of the corresponding `REQ` without an explicit dependency.

## Schema validation

- Before starting a handler for an incoming `REQ` or `ONCE`, the framework validates the message payload against the operation's `input` schema.
- Before sending an `ok` `RESP`, the framework validates the `result` value against the operation's `response` schema.

Validation failures:

- An invalid `REQ` is answered with an `exception` response whose text is `unexpected-message-format`. The handler is not started.
- An invalid `ONCE` is rejected and recorded as an warning log entry whose content identifies `unexpected-message-format`.
- An invalid `ok` `RESP` is answered with an `exception` response whose text is `unexpected-response-format` and is recorded as an error log entry.

Box configuration values supplied by the app must conform to each box's declared configuration schemas. A framework must reject an application whose supplied configuration does not satisfy those schemas.

## Exception mapping

Handler exceptions:

- If the inbound message was `REQ`, the framework sends a `RESP` with `status: exception` and a `text` carrying the exception name and message. The remote call stack is included when `runtime.sendRemoteExceptionStacks` is `true`.
- If the inbound message was `ONCE`, no response is sent. The exception is recorded as an error log entry.

Lifecycle exceptions:

- An exception that terminates `init` is fatal. The box transitions to `ERROR`.
- An exception that terminates `run` is fatal for the entry box. The exception is logged and application shutdown is requested.
- An exception that terminates `deinit` is logged as a fatal cleanup failure. Destruction continues with destruction of owned instances.

If the framework cannot resolve or instantiate a target box during `sendMsg` or `createBox`, an exception is thrown in the caller's execution. This is a local failure, not a remote `exception` response.

## Shutdown

`shutdown()` initiates an ordered destruction sequence:

1. The entry box enters `DEINIT`, rejects new ordinary work, and observes `isCancelled() == true` in its already-running handlers (including `run`).
2. After already-running handlers complete (including the one that called `shutdown()` if any), the entry box executes its `deinit` handler while framework-owned default instances remain available.
3. After the entry box's `deinit` completes, or after its destruction timeout is reached, the framework destroys all instances owned by the entry box, recursively and in reverse creation order at each owner.
4. The framework removes the entry box.
5. The framework destroys all remaining framework-owned default instances in reverse creation order.
6. Shutdown completes after all boxes have been destroyed.

For any box destroyed during the sequence, its own `deinit` handler runs before destruction of its owned instances unless its destruction timeout is reached.

## Destruction timeout

Every box has a destruction timeout. The application supplies the default via `runtime.destroyTimeoutMs`. A box may override the default through its own future runtime-binding mechanism; this spec does not define a per-box override.

- The timeout begins when the box enters `DEINIT`.
- The timeout covers waiting for already-running handlers to complete and executing the box's `deinit` handler.
- If the timeout is reached first, the framework records an error log entry, treats the box as fatally failed, and forcibly completes its logical removal. `deinit` is not dispatched if handler draining has not completed. How underlying executing work is stopped is framework-specific.

## Runtime tunables

Apps may supply the following keys in their `configuration` mapping. All keys are optional; values default to those documented here.

```yaml
runtime.defaultMsgTimeoutMs:
  required: false
  schema:
    type: integer
    minimum: 1
runtime.destroyTimeoutMs:
  required: false
  schema:
    type: integer
    minimum: 1
runtime.logLateResponses:
  required: false
  schema:
    type: boolean
runtime.sendRemoteExceptionStacks:
  required: false
  schema:
    type: boolean
```

Defaults when a key is absent:

- `runtime.defaultMsgTimeoutMs` — `10000`.
- `runtime.destroyTimeoutMs` — `5000`.
- `runtime.logLateResponses` — `false`.
- `runtime.sendRemoteExceptionStacks` — `true`.

`runtime.defaultMsgTimeoutMs` is the timeout applied to `sendMsg(REQ, ...)` calls that omit a per-call timeout.

`runtime.destroyTimeoutMs` is the default destruction timeout applied to every box.

`runtime.logLateResponses` controls whether `RESP` messages discarded after a request timeout are recorded as warning log entries.

`runtime.sendRemoteExceptionStacks` controls whether remote `exception` response `text` includes the remote call stack in addition to the exception name and message.

## Logging

Every box may emit log entries. The framework emits log entries for framework-detected failures.

Log categories are:

- `error`
- `warning`
- `normal`
- `debug`

Each log entry contains:

- its category;
- the log message content;
- the originating box name and instance id;
- a timestamp;
- the application identifier.

## Compatibility

A change to lifecycle, dispatch, methods, addressing, framework function contracts, schema validation timing, exception mapping, shutdown sequence, destruction timeout, tunable keys or their schemas, or logging structure is a runtime contract change that requires re-evaluation of every dependent app and framework unit.

## Runtime evaluation

When this spec changes, dependent apps must be re-evaluated to determine whether their composition, configuration, or behavior must change. Dependent `framework` units must be re-evaluated to determine whether they still implement the declared runtime version.

If evaluation changes a dependent unit's payload, that unit's version must be incremented.
