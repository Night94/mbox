---
mbox_unit: 1
unit: box
type: spec
version: 5
uses:
  spec: 1
  interface: 1
  schema: 2
---

# Box Spec

A box is a unit that defines a message-driven component.

A box has a defined responsibility and participates in communication contracts defined by interface units.

A box may provide interface operations, consume interface operations, and require app-provided configuration.

A box does not define the payload schema or communication contract of an operation it provides or consumes. The interface unit is authoritative for that contract.

## Mandatory box rules

A box must define its responsibility.

A box must identify the interface operations it provides.

A box must identify the interface operations it consumes.

A box must define its required dependencies through its use list.

A box must include `box` in its use list as its governing spec.

A box must include each interface unit whose operations it provides or consumes in its use list.

A box must not rely on another unit, spec, or contract that is absent from its use list.

A box must not select a concrete provider for a consumed interface operation unless a more specific spec permits that coupling. Provider selection belongs to app composition.

A box must not directly change another box's internal state.

A box must interact with other boxes through interface operations carried by runtime messages unless a more specific spec allows another interaction.

A box that provides an interface operation must describe any implementation-specific failure behavior or side effects in addition to the contract defined by the interface unit.

A box must identify any required configuration.

A box must identify any externally visible side effects.

## Provided and consumed operations

A provided operation is an interface operation the box implements for callers.

A consumed operation is an interface operation the box requires from a provider selected by app composition.

A box may both provide and consume operations from the same interface when its behavior requires it.

Providing an operation means the box must satisfy that interface operation's input, response, failure, and behavioral requirements.

Consuming an operation means the box must produce and interpret values according to that interface operation's contract.

## Definition format

Every box unit must contain a `## Definition` heading followed immediately by exactly one fenced `yaml` code block.

The definition block is normative and machine-readable. It is authoritative for the box's provided interface operations, consumed interface operations, configuration requirements, and externally visible side effects.

The required definition shape is:

```yaml
provides:
  - interface: <interface unit identifier>
    operations: [<operation name>]
consumes:
  - interface: <interface unit identifier>
    operations: [<operation name>]
configuration:
  <configuration key>:
    required: <true | false>
    schema: <schema>
sideEffects:
  - <externally visible side effect>
```

Every key shown above is required. `provides`, `consumes`, and `sideEffects` may be `[]`. `configuration` may be `{}`.

Each entry in `provides` and `consumes` must identify a unit of type `interface` that appears in the box use list at the version last evaluated and a non-empty set of operations defined by that interface.

Each `configuration` entry identifies one app-provided configuration value required or optionally used by the box. Its `schema` is an inline schema defined by the `schema` spec.

A box unit whose `configuration` mapping is not empty must include `schema` in its use list at the version last evaluated.

`sideEffects` describes effects visible outside the box, such as file changes, network access, user interface display, or audio output. It does not replace detailed prose explaining those effects.

A box definition does not embed operation input or response schemas. Those belong to the identified interface units.

Example:

```yaml
provides:
  - interface: arithmetic
    operations: [add]
consumes: []
configuration: {}
sideEffects: []
```

## Box payload requirements

A box payload must contain its definition block and should additionally explain:

1. Purpose
2. Responsibility boundary
3. State assumptions
4. Failure behavior
5. Test expectations

## Box evaluation

When a box dependency changes, the box must be reevaluated to determine whether its provided operations, consumed operations, configuration, responsibility boundary, side effects, failure behavior, or test expectations must change.

If box payload changes during evaluation, the box version must be incremented.
