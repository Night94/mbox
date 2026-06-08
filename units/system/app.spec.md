---
mbox_unit: 1
unit: app
type: spec
version: 6
uses:
  spec: 1
  box: 5
  interface: 1
  schema: 2
---

# App Spec

An app is a unit that defines a runnable MBOX application.

An app composes boxes and binds consumed interface operations to providing boxes within a working system boundary.

An app may define startup behavior, available boxes, configuration requirements, interface entry points, and external integration points.

## Mandatory app rules

An app must declare the boxes it requires through its use list.

An app must declare each interface unit it binds or exposes through its use list.

An app must bind each interface operation consumed by an included box to a providing box or to a declared external provider, unless a more specific spec defines another resolution mechanism.

An app binding must identify the consuming box, interface operations, and selected providing box or external provider.

An app must not bind an interface operation to a box that does not provide that operation.

An app must designate exactly one included box as its entry box.

An app must identify its externally visible runtime entry points. These are distinct from the bootstrap entry box and identify which interface operations are reachable from outside the app boundary at runtime.

An app must identify which interface operations may be provided from outside the app boundary.

An app must identify which interface operations it provides across the app boundary.

An app must not rely on a box that is absent from its use list.

An app must not redefine box behavior.

An app must not redefine interface contracts or runtime message delivery rules.

An app may constrain how boxes are composed within that app.

An app may provide configuration values required by boxes.

An app must provide every configuration value declared as required by each included box.

When an app supplies a configuration value declared by an included box, that value must conform to the inline schema declared by the box.

An app may define app level policies such as startup order, allowed transports, persistence requirements, logging requirements, or test requirements.

## Bindings

A binding connects one consuming box requirement to one provider for one or more operations of an interface within the app.

A provider may be an included box that provides the contract or an external integration point declared by the app.

An app may bind different consumers of the same interface operations to different providers.

A test app may bind consumed interface operations to a test double that provides the same operations.

The app owns composition decisions. An interface unit owns the communication contract, and a box unit owns its provided and consumed participation in that contract.

## Definition format

Every app unit must contain a `## Definition` heading followed immediately by exactly one fenced `yaml` code block.

The definition block is normative and machine-readable. It is authoritative for included boxes, interface bindings, app boundary contracts, and supplied configuration.

The required definition shape is:

```yaml
entryBox: <box unit identifier>
boxes:
  - <box unit identifier>
bindings:
  - consumer: <box unit identifier>
    interface: <interface unit identifier>
    operations: [<operation name>]
    provider: <box unit identifier>
externalProviders:
  - consumer: <box unit identifier>
    interface: <interface unit identifier>
    operations: [<operation name>]
    provider: <external provider identifier>
exposes:
  - interface: <interface unit identifier>
    operations: [<operation name>]
    provider: <box unit identifier>
configuration:
  <configuration key>: <structured value>
```

Every key shown above is required. Each collection may be empty and `configuration` may be `{}`.

`entryBox` must identify a unit of type `box` that appears in the `boxes` list and in the app use list.

Every box listed in `boxes`, and every consumer or internal provider box identified in `bindings`, `externalProviders`, or `exposes`, must appear in the app use list and must identify a unit of type `box`.

Every interface identified in `bindings`, `externalProviders`, or `exposes` must appear in the app use list and must identify a unit of type `interface`.

Each internal `binding` must connect operations in the consumer box's `consumes` list to the same operations in the provider box's `provides` list.

Each `externalProviders` entry satisfies identified consumed interface operations through a provider outside the app boundary.

Each `exposes` entry makes identified interface operations provided by an included box available outside the app boundary and identifies entry points visible to external consumers.

For each consumed interface operation of each included box, the app must contain exactly one matching internal binding or external provider entry unless a more specific spec defines dynamic or multiple-provider resolution.

For each required configuration entry of each included box, the app `configuration` mapping must contain a value under the same key. Values supplied for included box configuration entries must conform to those entries' schemas under the `schema` spec.

The format for framework-initiated startup behavior and executable implementation bindings is not defined here; an executable app must declare those according to applicable runtime specs when introduced.

Example:

```yaml
entryBox: main
boxes:
  - main
  - worker
bindings:
  - consumer: main
    interface: arithmetic
    operations: [add]
    provider: worker
externalProviders: []
exposes: []
configuration: {}
```

## Entry box

An app must designate exactly one included box as its entry box through the `entryBox` field of the definition block. The entry box is the application's bootstrap unit: the framework instantiates it, then dispatches its `run` invocation after `init` completes. `run` executes once, may send messages, may wait for responses, and may request application shutdown.

The entry box must appear in the `boxes` list.

`init`, `run`, and `deinit` are framework lifecycle invocations on a box and are not interface operations. They are not declared in a box's `provides` list. Detailed runtime semantics for these invocations belong to a future runtime spec.

## App payload requirements

An app payload must contain its definition block and should additionally explain:

1. Purpose
2. Startup behavior
3. Failure behavior
4. Test expectations

## App evaluation

When an app dependency changes, the app must be reevaluated to determine whether its boxes, bindings, external interface contracts, entry points, configuration, or app level behavior must change.

If app payload changes during evaluation, the app version must be incremented.
