---
mbox_unit: 1
unit: imap
type: box
version: 4
uses:
  box: 5
  imap-api: 2
---

# imap

Connects to an IMAP server over TLS to test connectivity, count messages, load message content non-destructively in sequence or timestamp order, and move single messages between folders.

## Definition

```yaml
provides:
  - interface: imap-api
    operations: [test-connection, count-messages, load-oldest, load-at, load-by-date-at, move-message]
consumes: []
configuration: {}
sideEffects:
  - Issues outbound TLS connections to IMAP servers on port 993.
  - Reads mailbox folders and, for `imap-api.move-message`, modifies mailbox state by moving messages and creating destination folders.
```

## Responsibility boundary

Owns the IMAP exchange for each provided call. It does not own credential storage, folder discovery beyond the operations defined here, message body parsing beyond plain or HTML text extraction, or attachment handling.

## State assumptions

Stateless. Each call opens, uses, and releases its own IMAP connection.

## Failure behavior

Operational failures are per operation in `imap-api`. Authentication and transport failures of read operations and `imap-api.move-message` propagate as exceptions per the framework's default behavior. `imap-api.test-connection` is the explicit exception to that pattern and reports authentication or transport failures via `success: false`. A 30-second internal timeout applies to mailbox operations; a 10-second timeout applies to `imap-api.test-connection`.

## Move compatibility

If the server provides the IMAP `MOVE` extension, message moves are atomic. Otherwise the .NET provider uses MailKit's UID-list move fallback, which copies the message, marks it deleted in the source, and expunges that UID. Without `UIDPLUS`, MailKit preserves unrelated messages already marked deleted while emulating the targeted expunge. This fallback is not atomic if a connection fails between commands.

## Test expectations

Each provided operation satisfies the interface test expectations against a reachable IMAP server. In particular, `imap-api.load-by-date-at` orders timestamped messages by ascending `Date` header timestamp and uses UID order for ties. `imap-api.test-connection` never throws for authentication or transport failures; the other provided operations may.
