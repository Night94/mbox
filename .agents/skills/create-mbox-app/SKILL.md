---
name: create-mbox-app
description: Create a runnable MBOX application — its entry box, composition, bindings, and optional concrete framework realization. Use when a request asks to add an app, compose existing boxes into a runnable application, create an app main box, or add an app's realization under a chosen framework.
---

# Create MBOX App

Create the application composition from Markdown contracts first, then add only
the concrete framework files needed to make that composition executable.

## Context Order

1. Read `/units/system/kernel.v1.md`.
2. Read `/units/system/app.spec.md` and `/units/system/box.spec.md`.
3. Read `/units/system/catalog.spec.md`, `/units/catalogs/common-boxes.catalog.md`, and
   `/units/catalogs/apps.catalog.md`. Use the common-box catalog to select candidate
   capabilities and the app catalog to locate a close existing composition.
4. Read `/units/system/runtime.spec.md` when the app relies on lifecycle, messaging,
   shutdown, timeout, configuration, or external-entry behavior.
5. Read the Markdown units for only the boxes and interfaces to be composed.
   Use their definitions to choose providers and establish bindings.
6. When producing an executable app, identify the target framework by listing
   `/impl/` and reading the chosen framework's unit at
   `/units/frameworks/<framework>/<framework>.framework.md`.
7. Only after the composition is settled, inspect one similar app's realization
   under `/impl/<framework>/apps/<example>/` for bootstrap and project
   conventions. Read a provider's implementation only to resolve a concrete
   compile or integration question.

Do not enumerate, search, or read `**/bin/**`, `**/obj/**`, or equivalent
build-output paths. Prefer commands that constrain discovery to unit Markdown:

```powershell
rg --files -g "*.md" units
rg -n "<box-or-interface>" -g "*.md" units
```

## Workflow

1. Describe the app purpose and select existing capability contracts and box
   providers through their Markdown units. Create a new reusable box using
   `$create-mbox-box` only when composition cannot express the requested
   behavior with existing boxes.
2. Design the app-specific entry box `<app>-main` as a small orchestration box:
   define its consumed operations and startup sequence without placing
   provider implementation behavior in it.
3. Create `/units/apps/<app>/<app>-main.box.md` and `/units/apps/<app>/<app>.app.md`.
   Obtain front matter, `uses`, definition blocks, bindings, exposures, and
   configuration shape from the kernel and governing specs.
4. For an executable app, create its realization under
   `/impl/<framework>/apps/<app>/` after reading the framework unit and one
   close example. Place the framework-prescribed deployment configuration
   (per-machine values, secrets, environment overrides) next to that
   realization, not in the intent tree. Wire the new app into the framework's
   native build and project configuration as that framework requires.
5. For an app created beneath `/units/apps`, add it to `/units/catalogs/apps.catalog.md`,
   include its current version in the catalog `uses`, and increment the
   catalog version because its payload changes. For a modified app, reevaluate
   its existing catalog entry: update its description and catalog version only
   when the catalog payload must change, and update only `uses` when merely
   recording the evaluated app version.
6. Evaluate direct Markdown dependencies affected by any new entry box,
   interface, reusable box, or app payload change; avoid broad repository
   surveys.

## Implementation Boundary

Do not inspect implementation code of several boxes or apps as a substitute for
reading the unit contracts. Concrete source is appropriate for matching the
chosen framework's bootstrap, project wiring, and handler API because those
details may not be formalized in system specs. Keep that investigation to the
closest example and expand only in response to a specific unresolved issue.

## Completion Check

- Verify app and entry-box units against the kernel and their governing spec
  units.
- Verify each consumed operation has the intended app-side provider resolution
  and required configuration is supplied.
- Verify `/units/catalogs/apps.catalog.md` contains exactly one current entry and
  matching dependency version for every in-scope app affected by this work.
- When adding code under `/impl/<framework>/`, verify the app is included in
  that framework's native build configuration and run the smallest meaningful
  build or tests without reading generated build output.
