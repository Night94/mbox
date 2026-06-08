---
mbox_unit: 1
unit: framework
type: spec
version: 2
uses:
  spec: 1
  runtime: 1
---

# Framework Spec

A framework is a unit that describes a concrete MBOX framework implementation.

A framework unit declares which runtime version(s) it implements, the host language or runtime environment it runs on, its process topology, how it discovers box implementations, and where it reads configuration from.

A framework unit does not define the runtime contract. The `runtime` spec is authoritative for that. A framework unit asserts that this implementation satisfies the named runtime version(s).

More than one framework unit may implement the same runtime version. An app that depends on a particular framework chooses which implementation runs it.

## Mandatory framework rules

A framework must identify at least one runtime version it implements.

A framework must identify its host language or runtime environment.

A framework must identify its process topology.

A framework must identify how box implementations are discovered for the boxes composed into an app that runs on it.

A framework must identify the configuration sources it reads at application startup.

A framework must include `framework` in its use list as its governing spec.

A framework must include each runtime version it implements in its use list.

A framework must not redefine the runtime contract.

## Definition format

Every framework unit must contain a `## Definition` heading followed immediately by exactly one fenced `yaml` code block.

The definition block is normative and machine-readable. It is authoritative for implemented runtime versions, host environment, process topology, box implementation discovery, and configuration sources.

The required definition shape is:

```yaml
implements:
  - runtime: <runtime version>
host: <host language or runtime identifier>
processTopology: <single-process | multi-process | distributed>
boxImplementationDiscovery:
  convention: <per-framework-subdirectory>
  frameworkSubdirectory: <framework unit identifier>
configurationSources:
  - <configuration source identifier>
```

Every key shown above is required.

`implements` must list at least one runtime version. Each listed runtime version must appear in the framework's use list at the version named.

`host` identifies the host language or runtime environment in a stable identifier form (for example `dotnet8`, `python3.12`). The set of accepted identifiers is not enumerated here; the value must be unique enough that another unit can refer to this implementation unambiguously.

`processTopology` is one of:

- `single-process` — all boxes run in one operating-system process.
- `multi-process` — boxes may run in multiple cooperating processes on one host.
- `distributed` — boxes may run across multiple hosts.

`boxImplementationDiscovery.convention` identifies the rule by which the framework locates the executable code for each box composed into an app:

- `per-framework-subdirectory` — for each box at `<box-dir>/`, executable code is located under `<box-dir>/<frameworkSubdirectory>/`. `frameworkSubdirectory` is required and must identify the framework unit implementing that code.

Additional conventions may be defined in future versions of this spec.

An executable app may place framework-specific bootstrap and project files under its own `<app-dir>/<frameworkSubdirectory>/` directory by the same convention. This permits one unit payload to have implementations for multiple frameworks without mixing their source or build metadata.

`configurationSources` lists the sources from which application configuration is loaded at startup, in priority order from highest to lowest. The defined source identifier is:

- `application.json` — a JSON file named `application.json` at the app's root directory.

Additional configuration source identifiers may be defined by future specs.

Example:

```yaml
implements:
  - runtime: 1
host: dotnet8
processTopology: single-process
boxImplementationDiscovery:
  convention: per-framework-subdirectory
  frameworkSubdirectory: mbox-dotnet
configurationSources:
  - application.json
```

## Framework payload requirements

A framework payload must contain its definition block and should additionally explain:

1. Purpose
2. Scope of the implementation
3. Host environment requirements
4. Known limitations relative to the implemented runtime version(s)

## Framework evaluation

When a framework dependency changes, the framework must be reevaluated to determine whether its implemented runtime versions, host environment, process topology, discovery rule, or configuration sources must change.

If the framework payload changes during evaluation, the framework version must be incremented.
