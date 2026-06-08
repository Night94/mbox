---
mbox_unit: 1
unit: smtp-api
type: interface
version: 1
uses:
  interface: 1
  schema: 2
---

# smtp-api

Sends an email message through an authenticated SMTP submission server.

## Definition

```yaml
operations:
  send:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        port: { type: integer, minimum: 1, maximum: 65535 }
        startTls: { type: boolean }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        from: { type: string, minLength: 1, maxLength: 320 }
        to: { type: string, minLength: 1, maxLength: 320 }
        subject: { type: string, maxLength: 998 }
        bodyText: { type: string }
      required: [host, port, startTls, user, pwd, from, to, subject, bodyText]
      additionalProperties: false
    response:
      type: object
      properties:
        serverResponse: { type: string }
      required: [serverResponse]
      additionalProperties: false
    failures: {}
    behavior:
      - Connects to `host` on `port` and upgrades the connection with STARTTLS when `startTls` is true.
      - Authenticates using the supplied user name and password.
      - Submits one plain-text message with the supplied sender, recipient, subject, and body.
      - Returns the SMTP server response after successful submission.
```

## Compatibility rules

Changing connection security selection, authentication input, message envelope behavior, or delivery response reporting requires reevaluation of providers and consumers.

## Test expectations

Against a reachable authenticated SMTP submission service, `send` submits exactly one plain-text message matching the requested sender, recipient, subject, and body; authentication, TLS, and submission failures propagate as exceptions.
