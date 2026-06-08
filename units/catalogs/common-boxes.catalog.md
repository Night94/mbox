---
mbox_unit: 1
unit: common-boxes
type: catalog
version: 2
uses:
  catalog: 1
  display: 3
  imap: 4
  mail-classifier: 3
  ollama: 3
  smtp: 2
  text-input: 3
  text-to-speech: 2
  worker: 2
---

# common-boxes

A discovery index of reusable boxes available for application composition.

## Definition

```yaml
scope:
  description: All reusable box units stored beneath /units/common.
  pathPrefix: /units/common
  unitTypes: [box]
  coverage: complete
catalog:
  - unit: display
    description: Displays single-line or scrollable multiline text in a local window.
  - unit: imap
    description: Reads and moves messages through a TLS-connected IMAP mailbox.
  - unit: mail-classifier
    description: Classifies loaded mail into destination folders using ordered rules and optional Ollama judgments.
  - unit: ollama
    description: Submits plain or schema-constrained text generation requests to an Ollama HTTP endpoint.
  - unit: smtp
    description: Sends plain-text email messages through an SMTP submission server.
  - unit: text-input
    description: Prompts the local user for editable single-line or multiline text.
  - unit: text-to-speech
    description: Speaks supplied text through a local neural voice model.
  - unit: worker
    description: Provides small arithmetic operations for framework concurrency, timeout, and failure demonstrations.
```
