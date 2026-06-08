# MBOX Agent Guide

This repository uses the MBOX unit system. Read `/units/system/kernel.v1.md` before interpreting or modifying any unit. The kernel is the source of truth for unit structure, file format, versioning, dependency evaluation, and payload change rules.

A Markdown file is an MBOX unit only if its YAML front matter declares the required unit fields defined by the kernel.

## Repository layout

The repository has two parallel trees:

- `/units/` — the **intent layer**. All MBOX units (Markdown). Source of truth for what exists and what it is for. Framework-agnostic and self-sufficient: a new runtime realization can be authored from this tree alone.
- `/impl/<framework>/` — **realizations** of the intent layer in concrete runtimes. Each framework follows its own native conventions. Zero or more such trees may exist; discover the available ones by listing `/impl/`.

Intent layout:

- `/units/system/kernel.v1.md` — kernel (not a unit).
- `/units/system/*.spec.md` — system specs.
- `/units/common/<box>/` — reusable box units and their interface units.
- `/units/frameworks/<framework>/<framework>.framework.md` — framework definition units.
- `/units/apps/<app>/` — application units and their entry-box units.
- `/units/catalogs/*.catalog.md` — discovery catalogs.

Deployment configuration (per-machine values, secrets, environment overrides) belongs to a specific impl tree, not to the intent layer.

## Discovery process

Start with the smallest relevant context.

1. Read `/units/system/kernel.v1.md`.
2. Inspect relevant `/units/system/*.spec.md` files for the task.
3. For composition or capability discovery, read the relevant catalog under `/units/catalogs/` and then inspect only candidate units.
4. Follow `uses` entries from the unit being edited.
5. Inspect implementation code under `/impl/<framework>/` only when the task requires concrete build wiring, runtime behavior, or a specific framework example not covered by units.

Do not enumerate generated build output under `**/bin/**`, `**/obj/**`, or equivalent paths. Exclude those from recursive commands.

## Repository skills

- `/.agents/skills/create-mbox-box/SKILL.md` — adding a reusable box and its interface.
- `/.agents/skills/create-mbox-app/SKILL.md` — adding an app, its entry box, and its framework realization.
- `/.agents/skills/maintain-mbox-integrity/SKILL.md` — checking and repairing unit dependency and catalog integrity.

## Editing expectations

- Preserve valid unit front matter. The unit header is authoritative over filename and directory conventions.
- Do not change a unit identifier or unit type.
- Do not increment a unit version unless the unit payload changes; when it does, update the version and identify dependent units that require reevaluation.
- When adding a unit, include complete front matter and a deliberate `uses` list. Prefer specific dependencies over broad ones.
- When removing a unit, identify dependent units that require reevaluation.
- When changing an app's realization in an impl tree, keep that tree's native build and project configuration consistent.
