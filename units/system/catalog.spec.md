---
mbox_unit: 1
unit: catalog
type: spec
version: 1
uses:
  spec: 1
---

# Catalog Spec

A catalog is a unit that provides a compact discovery index over a complete, declared collection of other units. It lets a reader identify candidate units from short descriptions before reading their full payloads.

A catalog is not authoritative for the behavior of any cataloged unit. Each cataloged unit remains authoritative for its identity, type, dependencies, and payload.

## Mandatory catalog rules

A catalog must include `catalog` in its use list as its governing spec.

A catalog must state what it catalogs through a complete scope definition.

A catalog must contain exactly one entry for each unit that matches its scope and no entries for units outside that scope.

A catalog must include each cataloged unit in its use list at the version last evaluated.

A catalog entry must identify a cataloged unit by its authoritative unit identifier and give a concise discovery description of that unit's current purpose or capability.

A catalog must not redefine the behavior, contracts, bindings, or configuration of a cataloged unit.

## Definition format

Every catalog unit must contain a `## Definition` heading followed immediately by exactly one fenced `yaml` code block.

The definition block is normative and machine-readable. It is authoritative for the catalog's scope and entries.

The required definition shape is:

```yaml
scope:
  description: <what collection this catalog covers>
  pathPrefix: <repository-root-relative directory path>
  unitTypes: [<unit type>]
  coverage: complete
catalog:
  - unit: <unit identifier>
    description: <concise discovery description>
```

Every key shown above is required.

`scope.description` must explain the collection in human-readable terms.

`scope.pathPrefix` identifies the repository directory subtree whose unit files are considered for membership. The path limits the catalog's collection; it does not determine a unit's identity or type.

`scope.unitTypes` must contain one or more unit types. A Markdown file beneath `pathPrefix` is in scope only when its YAML header declares `mbox_unit: 1` and its authoritative `type` is included in this list.

`scope.coverage` is `complete`. A catalog is an exhaustive index of its defined scope, not a curated subset.

`catalog` lists all matching units. Entries must be unique by `unit`, and each entry's unit must appear in the catalog unit's use list at its version last evaluated.

Example:

```yaml
scope:
  description: All reusable box units stored under /common.
  pathPrefix: /common
  unitTypes: [box]
  coverage: complete
catalog:
  - unit: text-input
    description: Prompts the local user for editable text input.
  - unit: display
    description: Shows supplied text in a local window.
```

## Discovery use

A consumer may read a catalog first to identify likely candidate units, then read only the candidate unit payloads and any required interface or governing spec units. A catalog description is a discovery aid and must not be used in place of a candidate unit's payload when interpreting behavior or composing dependencies.

## Catalog evaluation

Evaluation of a catalog checks both dependency freshness and scope integrity:

1. Compare every cataloged unit in `uses` with its current version. For a changed dependency, read that unit and determine whether its catalog description must change. Unchanged entries need not be reread for description maintenance.
2. Enumerate MBOX unit headers beneath `scope.pathPrefix` and filter by `scope.unitTypes`. Confirm that the resulting unit identifiers correspond exactly to the `catalog` entries and cataloged unit dependencies.
3. Add entries and dependencies for newly discovered in-scope units, and remove entries and dependencies for units that are no longer in scope or no longer exist.

Step 2 is a membership integrity check over unit headers, not a required full payload refresh. The payload of a newly discovered unit must be read to write its description; existing unchanged unit payloads do not need to be reread merely to establish membership.

Adding, removing, relocating, or changing the type of a unit within a catalog scope requires reevaluation of each affected complete catalog. An addition cannot be detected solely through a pre-existing `uses` edge because that edge does not yet exist.

When an existing cataloged dependency changes version, targeted evaluation may update only the affected description and the recorded dependency version when scope membership has not otherwise changed.

If evaluation changes only `uses`, the catalog version does not change under the kernel. If evaluation changes the definition payload, including any catalog entry or scope, the catalog version must be incremented.

## Integrity audits

Repository tooling may audit catalog scope integrity without waiting for a dependency version change. Such an audit detects missed membership reevaluations; it does not replace the requirement that a task adding, removing, relocating, or retyping an in-scope unit reevaluate affected catalogs as part of that change.
