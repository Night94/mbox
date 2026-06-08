---
mbox_unit: 1
unit: smtp-test
type: app
version: 2
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  smtp-test-main: 1
  text-input: 3
  smtp: 2
  text-input-api: 2
  smtp-api: 1
---

# smtp-test

An interactive SMTP submission check: collect text from the user, then send it as both the subject and body of one configured email message.

## Definition

```yaml
entryBox: smtp-test-main
boxes:
  - smtp-test-main
  - text-input
  - smtp
bindings:
  - consumer: smtp-test-main
    interface: text-input-api
    operations: [prompt]
    provider: text-input
  - consumer: smtp-test-main
    interface: smtp-api
    operations: [send]
    provider: smtp
externalProviders: []
exposes: []
configuration:
  smtp.host: "smtp.example.com"
  smtp.port: 587
  smtp.startTls: true
  smtp.user: "user@example.com"
  smtp.pwd: "<smtp-password>"
  smtp.from: "user@example.com"
  smtp.to: "recipient@example.com"
```

## Purpose

Provides a minimal end-to-end SMTP test with credentials and addressing supplied as application configuration rather than compiled into code. For the `mbox-dotnet` runtime, `application.json` carries the startup configuration values; the definition above declares the same required configuration according to the app specification.

## Startup behavior

The framework dispatches `run` to `smtp-test-main` after initialization. The entry box prompts for text and, after confirmation, sends exactly one configured email using that text as both subject and plain-text body.

## Failure behavior

Cancelling the input dialog shuts down without submitting mail. Connection, STARTTLS, authentication, or submission failures terminate the run and are recorded by the runtime.

## Test expectations

With valid settings in `application.json`, a confirmed prompt sends one matching email to the configured destination using authenticated SMTP submission over STARTTLS. Cancelling performs no SMTP send.
