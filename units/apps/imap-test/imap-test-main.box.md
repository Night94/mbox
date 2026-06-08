---
mbox_unit: 1
unit: imap-test-main
type: box
version: 5
uses:
  box: 5
  schema: 2
  imap-api: 2
  mail-classifier-api: 1
  display-api: 2
---

# imap-test-main

The entry box for the `imap-test` app. Scans `INBOX` in oldest message-timestamp-first order within the calendar year of its first loaded message until it has found 300 messages with no matching classifier rule, reaches a later year, or exhausts the folder. It moves each classified message into the returned folder and displays a live dispatch report as processing progresses.

## Definition

```yaml
provides: []
consumes:
  - interface: imap-api
    operations: [count-messages, load-by-date-at, move-message]
  - interface: mail-classifier-api
    operations: [classify]
  - interface: display-api
    operations: [show-window, show-string, use-multitext]
configuration:
  imap.host:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
  imap.user:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
  imap.pwd:
    required: true
    schema:
      type: string
      minLength: 0
      maxLength: 1024
sideEffects: []
```

## Responsibility boundary

Owns the load-classify-move sequence and the progress report shown on the display. Mailbox access, classification logic, and rendering belong to the bound providers.

## State assumptions

Stateless across invocations. Holds only the in-progress dispatch report for the current `run` execution.

## Failure behavior

A `mail-classifier-api.classify` response declaring `no-matching-rule` is treated as an expected non-match, produces a `not moved` dispatch line, and counts toward the 300-unmatched stopping condition. Other declared classify or move failures produce a `not moved` dispatch line followed by an error detail but do not count as unmatched. A loaded message outside the selected calendar year is not classified or moved and ends processing normally. A failure that prevents continuing the iteration terminates `run` with an exception.

## Startup behavior

The `run` invocation:

1. Reads `imap.host`, `imap.user`, and `imap.pwd` from configuration.
2. Sends `display-api.show-window` and `display-api.show-string` to show a progress message while loading.
3. Sends `imap-api.count-messages` with `folder: "INBOX"` to determine the iteration range.
4. Starting at the oldest message `Date` header timestamp, sends `imap-api.load-by-date-at` to load messages and records the calendar year of the first successfully loaded message as the processing year.
5. Sends each message in that calendar year as a `mail-classifier-api.classify` request until either 300 messages receive `no-matching-rule`, a loaded message belongs to a different calendar year, or all available messages have been examined. The first out-of-year message is not classified or moved.
6. For each successful classification, sends `imap-api.move-message` using the message's stable IMAP identity and the returned destination folder. The provider creates the destination folder if necessary.
7. After a successful move, continues at the same date-ordered index because the next source message has shifted into that position; after a message remains in `INBOX`, advances to the next index.
8. After each successfully loaded message is processed, appends one dispatch line in the form `<message date/time> <sender> <recipient> -> <destination>` or `<message date/time> <sender> <recipient> -> not moved`; any genuine error is appended after its dispatch line. It also reports the processing year and any stop at the following year's first loaded message.
9. Sends the accumulated report through `display-api.use-multitext` after each appended processing result so the display updates while messages are being handled.
10. Waits 60 seconds, then requests application shutdown.

## Test expectations

Against a reachable IMAP account with `INBOX` non-empty, the displayed report updates after each processed message in ascending message `Date` timestamp order, lists its message date/time, sender, recipient, and move result, reports expected non-matches as `not moved`, and lists any genuine errors. Processing applies only to messages in the calendar year of the first successfully loaded message and stops without classifying or moving the first subsequently loaded message from a later year. Processing also stops after the 300th `no-matching-rule` response, while matched messages moved before that threshold do not consume the limit. The application terminates cleanly approximately 60 seconds after the completed report is shown.
