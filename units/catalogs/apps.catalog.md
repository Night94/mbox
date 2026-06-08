---
mbox_unit: 1
unit: apps
type: catalog
version: 3
uses:
  catalog: 1
  autosort-refresh: 3
  bday: 1
  city-sights: 1
  hello-world: 2
  imap-test: 20
  name-meaning: 1
  ollama-chat: 1
  sentence-poem: 1
  single-mail-test: 3
  smtp-test: 1
  speak: 1
  worker-demo: 1
---

# apps

A discovery index of runnable applications in this repository.

## Definition

```yaml
scope:
  description: All runnable application units stored beneath /apps.
  pathPrefix: /apps
  unitTypes: [app]
  coverage: complete
catalog:
  - unit: autosort-refresh
    description: Produces conservative mailbox-sample-derived classification rules and conflict reports.
  - unit: bday
    description: Collects a birthday, asks Ollama for famous birthday matches, and speaks the answer.
  - unit: city-sights
    description: Collects a city name, asks Ollama for the five most important sightseeing sites, and speaks just their names.
  - unit: hello-world
    description: Shows a delayed hello-world message as a minimal display and runtime demonstration.
  - unit: imap-test
    description: Classifies and moves INBOX messages while displaying progress and results.
  - unit: name-meaning
    description: Collects a full name, asks Ollama for likely origins and meanings, and speaks a concise answer.
  - unit: ollama-chat
    description: Repeatedly prompts for text, displays Ollama replies, and speaks them aloud.
  - unit: sentence-poem
    description: Converts an entered sentence into an approximately 100-word Ollama-generated displayed poem.
  - unit: single-mail-test
    description: Experiments with editable Ollama classification prompts against selected INBOX messages.
  - unit: smtp-test
    description: Prompts for text and sends it as one configured SMTP email message.
  - unit: speak
    description: Repeatedly prompts for text and speaks it through local synthesis.
  - unit: worker-demo
    description: Exercises concurrency, timeout, declared-failure, and instance-lifecycle runtime behavior.
```
