---
mbox_unit: 1
unit: spec
type: spec
version: 1
uses: {}
---

# Spec Spec

A spec is a unit that defines rules for a unit type or for a concern shared by multiple unit types.

The `spec` unit governs spec units and defines how all higher-level unit types are connected to their governing specifications.

This unit is the bootstrap governing spec for units of type `spec`. It does not list itself in its use list.

## Governing specs

A unit type is usable only when its rules are defined by a governing spec.

For a unit type `<type>`, its governing spec is the unit with:

```yaml
unit: <type>
type: spec
```

A governing spec must define the meaning, mandatory payload rules, and evaluation requirements for units of its governed type.

A spec may instead define a supporting concern shared by multiple unit types. A supporting spec does not introduce a unit type unless units are created whose `type` matches that spec's unit identifier.

The kernel defines the common structure and dependency mechanics of all units. A spec must not redefine kernel rules.

## Mandatory spec rules

Every spec unit other than this bootstrap `spec` unit must include `spec` in its use list.

A spec that governs a type must identify the governed type by its own unit identifier and define the requirements for units of that type.

A supporting spec must identify the concern it defines and the kinds of units that may depend on it.

A spec must identify how changes to its payload affect dependent unit evaluation.

A spec may depend on governing specs or supporting specs required to define its concern.

## Mandatory typed unit rules

Every unit whose type is not `spec` must include its governing spec in its use list.

For a unit with:

```yaml
type: <type>
```

the use list must contain:

```yaml
uses:
  <type>: <governing spec version last evaluated>
```

The typed unit must satisfy the payload requirements defined by its governing spec.

A typed unit may include additional governing or supporting specs in its use list when its payload relies on their rules.

A typed unit must include every concrete unit dependency required by its payload in its use list.

## Type creation

To introduce a new unit type:

1. Add a spec unit whose unit identifier is the new type and whose type is `spec`.
2. Include `spec` in that governing spec's use list.
3. Define the new type's meaning, mandatory payload rules, and evaluation requirements.
4. Add typed units only after the governing spec exists.
5. Include the governing spec in each new typed unit's use list.

## Evaluation

When a governing spec changes, units of its governed type that list the changed version in their use list become stale according to the kernel.

When a supporting spec changes, dependent units become stale according to the kernel.

Evaluation of a stale typed unit must determine whether its payload and dependencies still satisfy all listed specs.

If evaluation changes only the use list to record evaluated spec versions, the typed unit version does not change.

If evaluation changes the typed unit payload, the typed unit version must be incremented.

## Invalid systems

An MBOX system is invalid if:

- a unit uses a type for which no governing spec exists;
- a unit whose type is not `spec` omits its governing spec from its use list;
- a spec unit other than the bootstrap `spec` unit omits `spec` from its use list; or
- a spec or typed unit contradicts the kernel.
