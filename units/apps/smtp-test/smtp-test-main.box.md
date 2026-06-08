---
mbox_unit: 1
unit: smtp-test-main
type: box
version: 1
uses:
  box: 5
  schema: 2
  text-input-api: 2
  smtp-api: 1
---

# smtp-test-main

The entry box for `smtp-test`. It asks the user for a short message and submits one email using that same text as subject and body.

## Definition

```yaml
provides: []
consumes:
  - interface: text-input-api
    operations: [prompt]
  - interface: smtp-api
    operations: [send]
configuration:
  smtp.host:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
  smtp.port:
    required: true
    schema:
      type: integer
      minimum: 1
      maximum: 65535
  smtp.startTls:
    required: true
    schema:
      type: boolean
  smtp.user:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
  smtp.pwd:
    required: true
    schema:
      type: string
      minLength: 0
      maxLength: 1024
  smtp.from:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 320
  smtp.to:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 320
sideEffects:
  - Writes SMTP send confirmation information to the application log.
```

## Responsibility boundary

Owns the interactive single-message test sequence and obtains endpoint, credentials, and envelope values from application configuration. Prompt rendering and SMTP transport belong to the bound providers.

## State assumptions

Stateless. One confirmed prompt produces one send attempt and then the application terminates.

## Failure behavior

Cancelling the prompt is an orderly termination path and sends no email. SMTP or configuration failures terminate `run` with an exception and trigger application shutdown through the runtime.

## Startup behavior

The `run` invocation:

1. Reads SMTP connection, authentication, sender, and recipient settings from application configuration.
2. Sends `text-input-api.prompt` requesting one line of text for the test message.
3. If the prompt reports `input-cancelled`, requests shutdown without sending mail.
4. Sends `smtp-api.send` with the entered text as both `subject` and `bodyText`.
5. Logs the returned SMTP response and requests orderly application shutdown.

## Test expectations

Cancelling the prompt sends no message. Confirming text against a reachable configured SMTP account sends exactly one email with matching subject and plain-text body to the configured recipient, records the successful server response, and terminates cleanly.
