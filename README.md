# MBOX

MBOX is an experiment in describing applications as framework-neutral units, then realizing those units in concrete runtimes.

The repository is split into two layers:

- `units/` is the intent layer. It contains Markdown units with YAML front matter that describe specs, reusable boxes, interfaces, frameworks, applications, and catalogs.
- `impl/` is the realization layer. It contains framework-specific code that makes selected units runnable in a concrete host.

An MBOX unit is not just a file in a folder. A Markdown file becomes a unit only when its front matter declares the required fields from `units/system/kernel.v1.md`: `mbox_unit`, `unit`, `type`, `version`, and `uses`. The unit header is authoritative; filenames and directories are conventions for humans and tools.

## The Basic Idea

An application unit says what app exists, which entry box it uses, which boxes it composes, which interfaces are bound, and what configuration it expects.

A box unit describes a reusable capability or an app-specific entry point.

An interface unit describes operations and schemas.

A framework unit describes how a runtime realizes units in a particular host environment.

The same intent tree can support multiple realizations. A framework can compile or host only the apps and boxes that make sense for that runtime, while the shared unit system keeps the definitions discoverable and dependency-checked.

## Repository Layout

```text
units/
  system/             Kernel and governing specs
  catalogs/           Discovery catalogs
  common/             Reusable boxes and interfaces
  frameworks/         Framework definition units
  apps/               Application and entry-box units

impl/
  mbox-dotnet/        .NET 8 realization
  web-pixel/          Static browser JavaScript realization
```

Catalogs in `units/catalogs/` provide compact discovery indexes. They are not the source of truth for behavior; each unit file remains authoritative.

## Frameworks

### `mbox-dotnet`

`mbox-dotnet` is a single-process .NET 8 runtime implementation. It loads framework-specific box implementations from the `impl/mbox-dotnet/` tree, dispatches operations through attributes, validates schemas, handles lifecycle and shutdown behavior, and uses `application.json` for app configuration.

Example apps include:

- `hello-world` - minimal display/runtime demonstration.
- `worker-demo` - concurrency, timeout, failure, and lifecycle behavior.
- `ollama-chat` - prompt/reply loop using Ollama.
- `bday`, `city-sights`, `name-meaning`, `sentence-poem` - small prompt-driven Ollama/TTS apps.
- `imap-test`, `single-mail-test`, `autosort-refresh`, `smtp-test` - mail-oriented experiments.
- `speak` - local text-to-speech loop.

Reusable common boxes include display, text input, worker, Ollama, IMAP, SMTP, mail classification, and text-to-speech boxes.

### `web-pixel`

`web-pixel` is a static browser-hosted JavaScript framework. It runs apps as moving "buis" inside a shared pixel world. The framework owns the world, movement, power, pills, body shape validation, browser UI, and local-storage persistence. Apps provide turn logic.

Example apps include:

- `john` - uses paid vision and retained pill targets to hunt visible pills.
- `tim` - conserves momentum and reshapes into a perpendicular sweeping body.
- `eve` - uses small survival pulses and reshapes into a blind sweeping net.
- `jen` - uses paid vision, retained pill targets, and exl grabbing that drives through pills.

The runnable browser realization starts at [`impl/web-pixel/index.html` on GitHub](https://github.com/Night94/mbox/blob/main/impl/web-pixel/index.html). When GitHub Pages is enabled for this repository, the running browser app is available at [https://night94.github.io/mbox/impl/web-pixel/index.html](https://night94.github.io/mbox/impl/web-pixel/index.html).

## Working With Units

Before interpreting or editing units, read:

```text
units/system/kernel.v1.md
```

The kernel defines unit structure, dependency evaluation, payload versioning, and the rule that changing a unit payload increments its version. Governing specs under `units/system/*.spec.md` define the rules for each unit type.

For integrity checks, the repository includes:

```powershell
python .agents/skills/maintain-mbox-integrity/scripts/scan_units.py .
```

That scanner checks unit front matter, dependency versions, governing specs, duplicate identifiers, missing dependencies, and catalog scope completeness.
