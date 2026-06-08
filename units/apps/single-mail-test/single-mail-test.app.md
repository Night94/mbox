---
mbox_unit: 1
unit: single-mail-test
type: app
version: 4
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  single-mail-test-main: 3
  imap: 4
  display: 3
  text-input: 3
  ollama: 3
  imap-api: 2
  display-api: 2
  text-input-api: 2
  ollama-api: 3
---

# single-mail-test

An interactive prompt experiment for mailbox samples: load the ten oldest-dated INBOX emails, display their headers and bodies, and repeatedly submit one editable instruction against each email through Ollama while logging schema-constrained confidence responses.

## Definition

```yaml
entryBox: single-mail-test-main
boxes:
  - single-mail-test-main
  - imap
  - display
  - text-input
  - ollama
bindings:
  - consumer: single-mail-test-main
    interface: imap-api
    operations: [load-by-date-at]
    provider: imap
  - consumer: single-mail-test-main
    interface: display-api
    operations: [show-window, show-string, use-multitext]
    provider: display
  - consumer: single-mail-test-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: single-mail-test-main
    interface: ollama-api
    operations: [generate]
    provider: ollama
externalProviders: []
exposes: []
configuration:
  imap.host: "imap.example.com"
  imap.user: "user@example.com"
  imap.pwd: "<imap-password>"
  ollama.baseUrl: "http://localhost:11434"
  ollama.model: "llama3:latest"
```

## Purpose

Provides a tight loop for tuning mail classification prompts against the same ten real messages, with no mailbox mutation and with each submitted instruction and parsed advertisement-confidence value visible together in the console log.

## Startup behavior

The framework dispatches `run` to `single-mail-test-main` after initialization. The entry box loads up to ten earliest messages in `INBOX` by message `Date` timestamp without attachments, displays their summary headers and bodies, and opens a multiline prompt seeded with an advertisement-confidence instruction. Each confirmation sends the same submitted prompt with each loaded email to Ollama with a JSON Schema requiring an integer advertisement confidence from 0 through 100. After logging one parsed response per email, it reopens the dialog prefilled with the last submitted prompt.

## Failure behavior

An empty folder, other IMAP read failure, generation failure, or invalid structured model response is displayed or logged as appropriate. Cancelling the input dialog is the orderly termination path. No operation moves, alters, or marks the email.

## Test expectations

Against reachable IMAP and Ollama servers supporting structured output, the app displays up to ten earliest-dated INBOX emails, submits each confirmed prompt once per displayed header/body content without attachments, parses and logs one JSON integer confidence value in the range 0 through 100 per email, and retains the prior prompt for the next edit. Cancelling shuts down cleanly.
