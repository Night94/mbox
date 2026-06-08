---
mbox_unit: 1
unit: autosort-refresh-main
type: box
version: 3
uses:
  box: 5
  schema: 2
  mail-classifier: 3
---

# autosort-refresh-main

The entry box for the `autosort-refresh` app. Reads autosort sample folders from an IMAP mailbox and emits a JSON report of deterministic sender-address classification rules, with limited domain promotion for repeatedly represented senders.

## Definition

```yaml
provides: []
consumes: []
configuration:
  autosort.sourceApplicationConfig:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 2048
  autosort.rootFolder:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
sideEffects:
  - Reads an application configuration file containing IMAP connection values.
  - Issues outbound TLS connections to the configured IMAP server on port 993.
  - Reads mailbox folder names and sample message headers without changing message state.
  - Writes a generated rule report to standard output.
```

## Responsibility boundary

Owns read-only sample discovery and conservative sender-address rule generation, including detection of sender domains sampled for different destinations. It does not move messages, modify classifier configuration, apply ambiguous rules, or persist report output.

## State assumptions

The app is run from the repository root so the configured source application path resolves consistently. Autosort categories are immediate child folders of `autosort.rootFolder`; each category may contain an immediate `samples` child folder.

## Failure behavior

Missing source configuration, IMAP authentication or transport failures, and missing autosort root folder terminate `run` with an exception. Categories without a `samples` folder or without samples are reported with no generated rules. A sender mailbox address observed in more than one category is reported as an issue and excluded because a `from` rule cannot select both destinations. A sender domain observed in more than one destination folder is reported as a `CONFLICTING DOMAIN RULE` issue, whether or not it meets the promotion threshold.

## Startup behavior

The `run` invocation:

1. Reads `autosort.sourceApplicationConfig` and obtains `imap.host`, `imap.user`, and `imap.pwd` from that JSON configuration file.
2. Connects to the IMAP account and enumerates immediate category folders beneath `autosort.rootFolder`.
3. Reads messages in each category's `samples` folder without marking or moving them.
4. Generates one `MATCH <category-folder> from <mailbox-address>` proposal for each unambiguous sender address observed in that category, quoting a destination folder when required by classifier syntax. It promotes to `from @<domain>` only when more than four distinct mailbox addresses at that domain all map to one destination, and flags every domain represented in more than one destination.
5. Emits an indented JSON report containing samples, proposed rules, and issues, then requests application shutdown.

## Test expectations

Against an account with autosort samples, the emitted report contains no message body or credential values, produces explicit sender-address rules by default, produces domain rules only above the unambiguous four-address threshold, identifies cross-destination sender domains as `CONFLICTING DOMAIN RULE` issues, and leaves the mailbox unchanged.
