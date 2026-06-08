---
mbox_unit: 1
unit: mail-classifier-api
type: interface
version: 1
uses:
  interface: 1
  schema: 2
---

# mail-classifier-api

Classifies a loaded mail message into a destination folder.

## Definition

```yaml
operations:
  classify:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        folder: { type: string }
        uid: { type: integer, minimum: 1 }
        uidValidity: { type: integer, minimum: 1 }
        from: { type: string }
        to: { type: string }
        subject: { type: string }
        date: { type: string }
        bodyText: { type: string }
      required: [folder, uid, uidValidity, from, to, subject, date, bodyText]
      additionalProperties: false
    response:
      type: object
      properties:
        folder: { type: string, minLength: 1, maxLength: 255 }
      required: [folder]
      additionalProperties: false
    failures:
      no-matching-rule: No configured rule classified the message.
    behavior:
      - Accepts the identity, headers, and body of a loaded mail message.
      - Returns the name of the selected destination folder.
      - Rule-based providers apply configured rules deterministically in order.
```

## Compatibility rules

Changing loaded-message input fields, returned destination semantics, or failure outcomes requires reevaluation of providers and consumers.

## Test expectations

Providers return a selected destination folder for a matching message and report `no-matching-rule` when no classification applies.
