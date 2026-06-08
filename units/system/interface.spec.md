---
mbox_unit: 1
unit: interface
type: spec
version: 1
uses:
  spec: 1
  schema: 2
---

# Interface Spec

An interface is a unit that defines a reusable group of related communication operations.

An interface contract is independent of any particular box implementation or app binding.

A box may provide interface operations by accepting and handling those communications.

A box may consume interface operations by initiating those communications.

An app binds consumed interface operations to providing boxes or exposes interface operations across the app boundary.

Messages are runtime communications that invoke or carry interface operations. A message is not a unit type defined by this spec.

## Interface units

Every concrete interface is a unit whose unit type is `interface`.

By the typed unit rules, an interface unit must depend on this governing spec and the schema spec:

```yaml
---
mbox_unit: 1
unit: <interface identifier>
type: interface
version: <positive integer>
uses:
  interface: 1
  schema: 2
---
```

The unit identifier names the capability contract. It should describe the capability rather than the particular box selected to provide it.

## Definition format

Every interface unit must contain an `## Definition` heading followed immediately by exactly one fenced `yaml` code block.

The definition block is normative and machine-readable. It is authoritative for the operations defined by the interface.

The required definition shape is:

```yaml
operations:
  <operation name>:
    kind: <request | command | event | notification>
    expectsResponse: <true | false>
    input: <schema>
    response: <schema | null>
    failures:
      <failure identifier>: <description>
    behavior:
      - <behavioral guarantee>
```

`operations` is required and must contain at least one operation. Operation names must be unique within the interface.

Each operation must contain every key shown above. `failures` may be `{}`. `behavior` may be `[]`.

`input` and a non-null `response` are inline schemas defined by the `schema` spec.

`expectsResponse: false` requires `response: null`.

`expectsResponse: true` permits either a response schema or `null`. A `null` response means completion is expected but no operation-specific successful result value is produced.

The definition block must not name a provider box, a consumer box, or an app binding unless that coupling is intrinsic to the capability contract and is described in the unit payload.

Example:

```yaml
operations:
  add:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        a: { type: integer }
        b: { type: integer }
      required: [a, b]
      additionalProperties: false
    response:
      type: object
      properties:
        sum: { type: integer }
      required: [sum]
      additionalProperties: false
    failures: {}
    behavior:
      - Returns the integer sum of the two input values.
```

## Mandatory interface rules

An interface must define its purpose.

An interface operation must define its interaction kind, input schema, response expectation, successful response schema when applicable, expected failure outcomes, and behavioral guarantees in the definition block.

An interface must define compatibility considerations for changing its operations.

An interface operation must be explicit enough that providers and consumers can be evaluated for compatibility.

An interface must not depend on unstated shared state between boxes.

An interface must not require one particular provider or consumer box unless that coupling is intrinsic to its contract and is declared explicitly.

## Providers, consumers, and bindings

The interface unit is authoritative for its operation contracts.

A providing box implements identified operations of an interface. Its use list must include the interface unit and its payload must identify the provided operations.

A consuming box relies on identified operations of an interface. Its use list must include the interface unit and its payload must identify the consumed operations.

Providing or consuming operations does not by itself select which box instances communicate in an app.

An app that connects a consumer to a provider must identify the interface operations in its binding and include the participating boxes and interface unit in its use list.

More than one box may provide the same interface operations, including alternate implementations and test doubles.

## Interface payload requirements

An interface payload must contain its definition block and should additionally explain:

1. Purpose
2. Compatibility rules
3. Test expectations

The interface unit identifier and operation name together identify an operation, for example `arithmetic.add`.

## Compatibility

An interface change affects every unit that provides, consumes, binds, exposes, routes, validates, transforms, stores, or documents affected operations.

Adding an operation does not require a payload change in boxes that do not provide or consume it, although dependency evaluation must record the new interface version.

Changing an existing operation's input schema, response expectation, response schema, failures, or behavioral guarantees requires reevaluation of units that provide or consume that operation.

Removing or renaming an operation requires reevaluation of every unit that references it.

## Interface evaluation

When an interface dependency changes, dependent units must be reevaluated to determine whether their operation provision, consumption, binding, external exposure, validation, storage, or failure behavior must change.

If a dependent unit payload changes during evaluation, that dependent unit version must be incremented.
