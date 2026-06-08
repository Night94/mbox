---
mbox_unit: 1
unit: mail-classifier
type: box
version: 3
uses:
  box: 5
  schema: 2
  mail-classifier-api: 1
  ollama-api: 3
---

# mail-classifier

Classifies a loaded mail message into a destination folder using an ordered list of configured rules. Some rules are deterministic header matches; others delegate semantic classification to a language model via `ollama-api.generate`.

## Definition

```yaml
provides:
  - interface: mail-classifier-api
    operations: [classify]
consumes:
  - interface: ollama-api
    operations: [generate]
configuration:
  Classifier.Rules:
    required: true
    schema:
      type: array
      items:
        type: string
        minLength: 1
      minItems: 1
  ollama.baseUrl:
    required: false
    schema:
      type: string
      minLength: 1
      maxLength: 2048
  ollama.model:
    required: false
    schema:
      type: string
      minLength: 1
      maxLength: 255
sideEffects: []
```

## Responsibility boundary

Owns the rule evaluation pipeline that picks a destination folder for one supplied message. It does not own loading messages from a mailbox, performing the move, persisting rule history, or selecting which Ollama instance to use.

## State assumptions

Reads rule and Ollama configuration at evaluation time. Holds no per-message state across calls.

## Failure behavior

Declares `no-matching-rule` on `mail-classifier-api.classify` when no rule matches. Invalid or incomplete rule configuration, missing Ollama configuration required by an `ASK` rule, and Ollama errors during `ASK` evaluation propagate as exceptions per the framework's default behavior. An unrecognized model answer to an `ASK` rule is logged as a warning and treated as a non-match so the next rule is considered.

## Rule format

Each entry in `Classifier.Rules` is a single-line directive evaluated in array order. The first matching rule selects the destination folder.

```text
MATCH <folder-name> <header-name> <match-value>
ASK <folder-name> <criterion>
```

`MATCH` performs a case-insensitive substring comparison against `<header-name>`. Supported header names are `from`, `to`, `subject`, and `date`. `<match-value>` may contain spaces and consumes the remainder of the line.

`ASK` submits `<criterion>`, the supplied message headers, and up to the first 1000 UTF-8 bytes of `bodyText` to `ollama-api.generate` using `ollama.baseUrl` and `ollama.model` from configuration. The model is instructed to return only `MATCH` or `NO_MATCH`. A clear positive answer selects the rule's folder; a negative or unrecognized answer continues to the next rule.

For either rule command, `<folder-name>` may be a JSON-quoted string when the destination contains whitespace or characters requiring quoting, for example `MATCH "INBOX.autosort.rental cars" from @europcar.de`.

## Test expectations

A `MATCH` rule whose header substring is present in the corresponding input field selects that rule's folder, case-insensitively. An `ASK` rule whose criterion the model answers with `MATCH` selects that rule's folder. With no matching rule, the box reports `no-matching-rule`. Rules are evaluated in array order, and the first matching rule wins.
