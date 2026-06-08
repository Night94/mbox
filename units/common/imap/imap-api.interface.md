---
mbox_unit: 1
unit: imap-api
type: interface
version: 2
uses:
  interface: 1
  schema: 2
---

# imap-api

Provides IMAP connectivity checks, non-destructive message reading, and message moves.

## Definition

```yaml
operations:
  test-connection:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
      required: [host, user, pwd]
      additionalProperties: false
    response:
      type: object
      properties:
        success: { type: boolean }
        message: { type: string }
      required: [success, message]
      additionalProperties: false
    failures: {}
    behavior:
      - Opens a TLS connection to `host:993`, authenticates with `user` and `pwd`, and disconnects.
      - Reports authentication or transport failures through `success: false`, not as a declared failure or exception.
  count-messages:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        folder: { type: string, minLength: 1, maxLength: 255 }
      required: [host, user, pwd, folder]
      additionalProperties: false
    response:
      type: object
      properties:
        folder: { type: string }
        count: { type: integer, minimum: 0 }
      required: [folder, count]
      additionalProperties: false
    failures:
      unknown-folder: "`folder` does not exist on the server."
    behavior:
      - Opens `folder` read-only and returns its total message count.
      - Does not change any message state.
  load-oldest:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        folder: { type: string, minLength: 1, maxLength: 255 }
      required: [host, user, pwd, folder]
      additionalProperties: false
    response:
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
    failures:
      unknown-folder: "`folder` does not exist on the server."
      folder-empty: "`folder` contains no messages."
    behavior:
      - Opens `folder` read-only and reads its lowest-sequence message.
      - Returns stable identity (`folder`, `uidValidity`, `uid`), summary headers, and the plain-text body when present, otherwise the HTML body, otherwise an empty string.
      - Excludes attachments from `bodyText`.
      - Does not change the message Seen flag.
  load-at:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        folder: { type: string, minLength: 1, maxLength: 255 }
        index: { type: integer, minimum: 0 }
      required: [host, user, pwd, folder, index]
      additionalProperties: false
    response:
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
    failures:
      unknown-folder: "`folder` does not exist on the server."
      message-index-out-of-range: "`index` does not identify a message in the current folder."
    behavior:
      - Opens `folder` read-only and reads the zero-based sequence `index`, with index zero matching `load-oldest`.
      - Returns the same stable identity and content shape as `load-oldest`.
      - Does not change the message Seen flag.
  load-by-date-at:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        folder: { type: string, minLength: 1, maxLength: 255 }
        index: { type: integer, minimum: 0 }
      required: [host, user, pwd, folder, index]
      additionalProperties: false
    response:
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
    failures:
      unknown-folder: "`folder` does not exist on the server."
      message-index-out-of-range: "`index` does not identify a message in the current folder."
    behavior:
      - Opens `folder` read-only and reads zero-based `index` after ordering messages by their `Date` header timestamp from oldest to newest.
      - Uses stable message UID order to break equal timestamps.
      - Returns the same stable identity and content shape as `load-oldest`.
      - Does not change the message Seen flag.
  move-message:
    kind: request
    expectsResponse: true
    input:
      type: object
      properties:
        host: { type: string, minLength: 1, maxLength: 255 }
        user: { type: string, minLength: 1, maxLength: 255 }
        pwd: { type: string, minLength: 0, maxLength: 1024 }
        sourceFolder: { type: string, minLength: 1, maxLength: 255 }
        uid: { type: integer, minimum: 1 }
        uidValidity: { type: integer, minimum: 1 }
        destinationFolder: { type: string, minLength: 1, maxLength: 255 }
      required: [host, user, pwd, sourceFolder, uid, uidValidity, destinationFolder]
      additionalProperties: false
    response:
      type: object
      properties:
        sourceFolder: { type: string }
        destinationFolder: { type: string }
        uid: { type: integer, minimum: 1 }
        destinationCreated: { type: boolean }
      required: [sourceFolder, destinationFolder, uid, destinationCreated]
      additionalProperties: false
    failures:
      unknown-folder: "`sourceFolder` does not exist on the server."
      stale-uid-validity: The source folder UIDVALIDITY differs from the supplied identity.
      invalid-message-uid: "`uid` cannot be represented for IMAP lookup."
      same-source-and-destination: "`destinationFolder` is the same folder as `sourceFolder`."
    behavior:
      - Opens `sourceFolder` read-write and moves the identified message into `destinationFolder`.
      - Creates a missing destination folder before the move and reports that creation.
      - Validates source UIDVALIDITY before performing the move.
```

## Compatibility rules

Changing folder access guarantees, stable identity behavior, destination creation, or operation failure outcomes requires reevaluation of providers and consumers of affected operations.

## Test expectations

Read operations leave server message state unchanged; `load-by-date-at` returns messages in ascending `Date` timestamp order; move relocates the identified message after UIDVALIDITY validation; connection testing reports connectivity outcomes without throwing for authentication or transport failures.
