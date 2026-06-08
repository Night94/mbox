---
mbox_unit: 1
unit: smtp
type: box
version: 2
uses:
  box: 5
  smtp-api: 1
---

# smtp

Connects to an SMTP submission server and sends plain-text email messages.

## Definition

```yaml
provides:
  - interface: smtp-api
    operations: [send]
consumes: []
configuration: {}
sideEffects:
  - Opens outbound SMTP network connections and sends email messages through the configured server.
  - Writes detailed SMTP connection, authentication, submission, and redacted protocol diagnostic information to the application log.
```

## Responsibility boundary

Owns the SMTP connection, STARTTLS upgrade when requested, authentication exchange, and submission of the provided message. It does not store credentials, choose recipients, or generate message content.

The `mbox-dotnet` implementation logs transport phases and a MailKit protocol transcript for diagnosis. Authentication secrets are redacted before the transcript is written to the application log.

## State assumptions

Stateless. Each request creates and closes its own authenticated SMTP session.

## Failure behavior

Authentication, transport, TLS negotiation, address parsing, and mail submission failures propagate as exceptions per the framework default behavior. A 30-second internal timeout applies to each send operation.

## Test expectations

Each successful request results in one submitted plain-text message and a returned SMTP server response. A failed authentication or transport attempt does not report successful submission. Diagnostic logs identify the failed phase and contain no supplied password or encoded authentication password value.
