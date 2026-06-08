---
mbox_unit: 1
unit: mbox-dotnet
type: framework
version: 3
uses:
  framework: 2
  runtime: 1
---

# mbox-dotnet

A single-process .NET 8 implementation of the MBOX runtime contract.

## Definition

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

## Purpose

The reference framework implementation used by the apps in this repository. Runs every box in a single .NET 8 process and resolves framework-specific code through each unit's `mbox-dotnet/` subdirectory.

## Scope of the implementation

Implements the full `runtime` v1 contract: lifecycle, message dispatch, addressing, framework functions, schema validation, exception mapping, shutdown, destruction timeout, runtime tunables, and logging.

## Implementation bindings

The `mbox-dotnet/` subdirectory convention is a build-time discovery rule. An app project compiles the `mbox-dotnet/` source files for the boxes it composes and passes its resulting assembly to the runtime. At startup, the runtime resolves box implementations from `[BoxImplementation("<unit>")]` attributes and resolves provided operation handlers from `[OperationHandler("<interface>", "<operation>")]` attributes in those loaded assemblies.

For JSON values handled by this implementation, a value governed by a `binary` schema is represented as a Base64-encoded JSON string. Schema byte-length constraints apply to the decoded bytes.

## Host environment requirements

A .NET 8 runtime on the host. No process supervisor, message broker, or external coordinator is required.

## Known limitations

- Single-process only. Boxes cannot be split across processes or hosts.
- One host language. Implementations selected for this framework must be provided as .NET 8 code under each unit's `mbox-dotnet/` subdirectory; non-.NET implementations are not loaded.
- No transport encoding negotiation; all message passing is in-process.
- Destruction timeout causes logical removal and error logging, but .NET `Task` executions already in progress are not forcibly terminated; handlers must observe cancellation cooperatively or finish naturally.
