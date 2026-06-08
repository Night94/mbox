---
name: maintain-mbox-integrity
description: Audit and repair MBOX unit integrity by evaluating stale dependency edges, governing-spec compliance, catalog scope completeness, and necessary catalog rebuilds. Use when asked to check, maintain, refresh, repair, validate, or reevaluate MBOX units or the repository dependency graph.
---

# Maintain MBOX Integrity

Evaluate the unit system dependency-first, and expand work only when a
governing spec requires an integrity check that dependency edges cannot detect.

## Required Context

1. Read `/units/system/kernel.v1.md`.
2. Read `/units/system/spec.spec.md`.
3. Read only the governing specs for reported affected unit types.
4. Read `/units/system/catalog.spec.md` when catalogs exist or catalog issues are
   reported.

Do not search or read `**/bin/**`, `**/obj/**`, or `**/models/**`.

## Inventory Pass

Run the bundled scanner from the repository root:

```powershell
python .agents/skills/maintain-mbox-integrity/scripts/scan_units.py .
```

The scanner reads unit front matter and catalog definitions, not implementation
source. It identifies dependency-version mismatches, missing dependencies,
governing-spec omissions, duplicate unit identifiers, and incomplete catalog
scopes.

Treat its output as a work queue, not as semantic evaluation. A unit with an
updated dependency still requires judgment under its governing spec.

## Evaluation Workflow

1. Resolve structural blockers first: malformed units, duplicate identifiers,
   absent dependencies, or absent governing specs.
2. For each unit whose recorded dependency version differs from the current
   dependency version, read the dependent unit, the changed dependency, and
   the dependent unit's governing spec. Decide whether the dependency change
   affects its payload.
3. When the payload remains valid, update only the `uses` version. Do not
   increment the unit version.
4. When the payload must change, edit it, increment its version once, update
   evaluated dependency versions, and then evaluate units that depend on the
   newly changed unit.
5. For every catalog, enforce its declared complete scope. Use header
   enumeration to detect additions or removals; read full payloads only for
   new members or members whose descriptions might have changed.
6. If a catalog has isolated membership or description changes, patch only
   those entries. Rebuild the catalog entries from its declared scope when
   entries are broadly missing, duplicated, contradictory, or no longer a
   trustworthy baseline.
7. Repeat the inventory pass until no mechanical issues remain. Use
   `--fail-on-issues` for the final check.

## Repair Boundaries

- Preserve authoritative `unit` and `type` fields.
- Apply kernel versioning rules exactly: only payload changes increment a
  version; `uses` refreshes alone do not.
- Keep catalog descriptions concise and discovery-oriented; do not copy
  contracts or app configuration into them.
- Avoid reading framework source unless a repaired unit also requires concrete
  implementation or build validation.
- Do not silently repair ambiguous semantic conflicts. Report them when the
  correct payload cannot be inferred from the affected units and specs.

## Completion Check

- Run:

```powershell
python .agents/skills/maintain-mbox-integrity/scripts/scan_units.py . --fail-on-issues
```

- Summarize units whose payloads changed and units whose `uses` were refreshed.
- Identify any repaired catalog membership and any remaining semantic issue
  that needs a design decision.
