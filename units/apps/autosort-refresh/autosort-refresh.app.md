---
mbox_unit: 1
unit: autosort-refresh
type: app
version: 3
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  autosort-refresh-main: 3
---

# autosort-refresh

Reads the `INBOX.autosort` sample hierarchy and emits conservative address-specific deterministic rules, with limited domain promotion and explicit cross-destination domain conflict reporting, that can be used to refresh the mail-classifier configuration.

## Definition

```yaml
entryBox: autosort-refresh-main
boxes:
  - autosort-refresh-main
bindings: []
externalProviders: []
exposes: []
configuration:
  autosort.sourceApplicationConfig: "apps/imap-test/application.json"
  autosort.rootFolder: "INBOX.autosort"
```

## Purpose

Provides a repeatable read-only evidence-gathering step for maintaining autosort-derived classifier rules without embedding mailbox inspection logic in a conversational maintenance session.

## Startup behavior

The framework dispatches `run` to `autosort-refresh-main`, which reads IMAP credentials from the configured classifier application's configuration, inspects sample folders, prints a JSON rule report, and shuts down.

## Failure behavior

A configuration, file, or IMAP failure terminates `run` and triggers framework shutdown. Identical sender addresses mapped to multiple categories are included as report issues and excluded from derived rules. Domains sampled for multiple destinations are reported as `CONFLICTING DOMAIN RULE` issues rather than being turned into broad rules.

## Test expectations

When executed from the repository root against the configured account, the app prints a read-only JSON report for the current sample hierarchy, including explicit cross-destination domain conflict issues when present, and terminates cleanly.
