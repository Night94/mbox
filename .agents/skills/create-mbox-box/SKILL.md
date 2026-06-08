---
name: create-mbox-box
description: Create a new reusable MBOX box, add or select its interface unit, and optionally implement it for a chosen framework. Use when a request asks to add a box/component/service/provider under common, define operations it provides or consumes, introduce its capability contract, or add its realization under a framework.
---

# Create MBOX Box

Create a reusable MBOX box from its contract outward. Keep discovery centered
on unit Markdown; open framework-specific code only for the requested executable
implementation.

## Context Order

1. Read `/units/system/kernel.v1.md`.
2. Read `/units/system/box.spec.md`; read `/units/system/interface.spec.md` when an
   interface will be added or used.
3. Read `/units/system/catalog.spec.md` and `/units/catalogs/common-boxes.catalog.md` to
   discover existing reusable capabilities before creating a new common box.
4. Read `/units/system/schema.spec.md` only when authoring schemas or box
   configuration.
5. Inspect only the relevant existing interface and box Markdown units under
   `/units/common/`. Select one close Markdown example only when its shape is
   useful.
6. For executable implementation work, identify the target framework by listing
   `/impl/`, then read `/units/frameworks/<framework>/<framework>.framework.md`
   and inspect at most one close implementation under
   `/impl/<framework>/common/<example>/` unless a build error makes further
   targeted investigation necessary.

Do not search or read `**/bin/**`, `**/obj/**`, or equivalent build-output
paths. Prefer commands that constrain discovery to unit Markdown:

```powershell
rg --files -g "*.md" units
rg -n "<term>" -g "*.md" units
```

## Workflow

1. Define the responsibility boundary and determine whether an existing
   interface already owns each needed operation.
2. If a capability contract is new, create its interface unit beside the box
   before defining the provider or consumer. Use the interface spec as the
   format authority.
3. Create `/units/common/<box>/<box>.box.md` and, when needed,
   `/units/common/<box>/<interface>.interface.md`. Derive front matter, `uses`, and
   definition blocks from the kernel and governing specs; do not duplicate
   schemas from an interface into the box.
4. Capture purpose, responsibility boundary, state assumptions, failure
   behavior, side effects, configuration, and test expectations in the unit
   payload where relevant.
5. Implement `/impl/<framework>/common/<box>/` only when requested or needed by
   a runnable app. Let the chosen framework unit and one targeted source
   example determine source layout and host-language bindings.
6. For a box created beneath `/units/common`, add it to
   `/units/catalogs/common-boxes.catalog.md`, include its current version in the
   catalog `uses`, and increment the catalog version because its payload
   changes. For a modified common box, reevaluate its existing catalog entry:
   update its description and catalog version only when the catalog payload
   must change, and update only `uses` when merely recording the evaluated box
   version.
7. Check the direct Markdown dependents affected by a new or changed interface
   or box. Evaluate and update only those whose contracts or recorded
   dependencies require it.

## Implementation Boundary

Do not read implementation source in unrelated boxes to learn the MBOX model.
The specifications and relevant unit payloads own that reasoning. Read the
chosen framework's unit first when implementing; use a similar provider's files
under `/impl/<framework>/common/<example>/` only to match concrete attribute,
handler, dependency, and test patterns that the framework unit does not fully
spell out.

## Completion Check

- Verify new or modified units against `/units/system/kernel.v1.md` and their
  governing spec units.
- Verify declared interface operations against the intended provider or
  consumer unit relationship.
- Verify `/units/catalogs/common-boxes.catalog.md` contains exactly one current
  entry and matching dependency version for every in-scope common box affected
  by this work.
- Run the smallest meaningful framework build or tests when source code was
  added, without examining generated build output.
