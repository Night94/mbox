---
mbox_unit: 1
unit: apps
type: catalog
version: 5
uses:
  catalog: 1
  autosort-refresh: 3
  bday: 2
  city-sights: 2
  eve: 6
  hello-world: 2
  imap-test: 21
  jen: 1
  john: 12
  name-meaning: 2
  ollama-chat: 2
  sentence-poem: 2
  single-mail-test: 4
  smtp-test: 2
  speak: 1
  tim: 11
  worker-demo: 1
---

# apps

A discovery index of runnable applications in this repository.

## Definition

```yaml
scope:
  description: All runnable application units stored beneath /units/apps.
  pathPrefix: /units/apps
  unitTypes: [app]
  coverage: complete
catalog:
  - unit: autosort-refresh
    description: Produces conservative mailbox-sample-derived classification rules and conflict reports.
  - unit: bday
    description: Collects a birthday, asks Ollama for famous birthday matches, and speaks the answer.
  - unit: city-sights
    description: Collects a city name, asks Ollama for the five most important sightseeing sites, and speaks just their names.
  - unit: eve
    description: Uses low-speed survival pulses while reshaping a blue-exl bui into a blind sweeping net in the browser-hosted web-pixel world.
  - unit: hello-world
    description: Shows a delayed hello-world message as a minimal display and runtime demonstration.
  - unit: imap-test
    description: Classifies and moves INBOX messages while displaying progress and results.
  - unit: jen
    description: Uses paid vision, retained visible-pill targets, targeted exl grabbing, and lane sweeping in the browser-hosted web-pixel world.
  - unit: john
    description: Uses paid vision, retained visible-pill targets, pill feedback, and targeted exl movement to chain hunts through visible pills with a field-sweeping fallback in the browser-hosted web-pixel world.
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
  - unit: tim
    description: Moves a green-exl bui in a conservative straight momentum line while reshaping into a perpendicular sweeper in the browser-hosted web-pixel world.
  - unit: worker-demo
    description: Exercises concurrency, timeout, declared-failure, and instance-lifecycle runtime behavior.
```
