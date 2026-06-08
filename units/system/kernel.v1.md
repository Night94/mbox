# MBOX Kernel v1

This document defines the MBOX unit system and the MBOX unit file format.

The kernel is not a unit.

All units are interpreted according to this kernel version.

## 1. Unit system

An MBOX system consists of units.

Each unit has:

1. A unit identifier
2. A unit type
3. A version
4. A use list
5. A payload

The unit identifier and unit type are immutable after creation.

The version represents the payload version.

Changing a unit payload increments the unit version.

The use list records the other units this unit depends on and the dependency versions last evaluated.

When a unit is removed, all dependent units become stale.

When a unit version changes, all dependent units become stale.

Evaluation is unit type specific.

During evaluation, a unit is checked against the current versions of its dependencies.

Evaluation may update the use list without incrementing the unit version.

If evaluation changes the unit payload, the unit version must be incremented.

## 2. Unit file format

A unit file is a Markdown file with YAML front matter.

Each unit file must begin with a YAML header:

```yaml
---
mbox_unit: 1
unit: <unit identifier>
type: <unit type>
version: <positive integer>
uses:
  <unit identifier>: <positive integer>
---
```

The Markdown content after the YAML header is the unit payload.

Every unit file must define:

```yaml
mbox_unit: 1
unit: <unit identifier>
type: <unit type>
version: <positive integer>
uses: {}
```

## 3. Field meanings

`mbox_unit` identifies the MBOX kernel and unit file format version.

`unit` is the authoritative unit identifier.

`type` is the authoritative unit type.

`version` is the payload version.

`uses` maps dependency unit identifiers to the dependency versions last evaluated.

## 4. Authority

The unit identifier in the header is authoritative.

The unit type in the header is authoritative.

The filename is not authoritative.

The directory path is not authoritative.

Tools may warn when a filename or directory location does not match the recommended convention, but they must read the unit identity and type from the header.

## 5. Payload and versioning

The payload is the Markdown content after the YAML header.

Changing the payload increments `version`.

Changing only `uses` does not increment `version`.

Changing `unit` or `type` is not allowed.

## 6. Recommended repository convention

Kernel files should be stored under `/kernel`.

Unit files should be stored under `/units`.

Unit filenames should follow this convention:

```text
<unit>.<type>.md
```

Examples:

```text
messaging.spec.md
architecture.spec.md
imap.box.md
imap-test.app.md
```

Optional grouping directories may be used for readability:

```text
/units/specs/messaging.spec.md
/units/boxes/imap.box.md
/units/apps/imap-test.app.md
```

Directory grouping is for human navigation only.

A unit's type is determined by the `type` field in the YAML header, not by its directory.

A unit's identifier is determined by the `unit` field in the YAML header, not by its filename.
