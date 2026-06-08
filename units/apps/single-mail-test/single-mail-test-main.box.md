---
mbox_unit: 1
unit: single-mail-test-main
type: box
version: 3
uses:
  box: 5
  schema: 2
  imap-api: 2
  display-api: 2
  text-input-api: 2
  ollama-api: 3
---

# single-mail-test-main

The entry box for `single-mail-test`. It keeps up to ten earliest-dated INBOX messages visible while repeatedly collecting an editable classification prompt and logging parsed, schema-constrained Ollama confidence responses for each message.

## Definition

```yaml
provides: []
consumes:
  - interface: imap-api
    operations: [load-by-date-at]
  - interface: display-api
    operations: [show-window, show-string, use-multitext]
  - interface: text-input-api
    operations: [prompt]
  - interface: ollama-api
    operations: [generate]
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
  ollama.baseUrl:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 2048
  ollama.model:
    required: true
    schema:
      type: string
      minLength: 1
      maxLength: 255
sideEffects:
  - Writes submitted instructions, parsed Ollama confidence values, and response failures to the application console log.
```

## Responsibility boundary

Owns the ten-message experiment loop and prompt construction. IMAP reading, desktop display, prompt dialog rendering, and Ollama transport remain responsibilities of the bound providers.

## State assumptions

The loaded sample of up to ten messages is held unchanged for the lifetime of `run`. The last confirmed instruction text is retained only in memory and supplied as the next dialog's initial value.

## Failure behavior

If loading messages returns an operational failure other than reaching the end of a non-empty folder before ten messages, the display shows that failure and the app shuts down. Prompt cancellation shuts down cleanly. Ollama and malformed structured-response failures are written to the log and the prompt loop continues.

## Startup behavior

The `run` invocation:

1. Opens the display and loads indices zero through nine of `INBOX` using `imap-api.load-by-date-at`, whose order is ascending message `Date` timestamp, stopping normally if a non-empty folder has fewer than ten messages.
2. Displays each loaded message's `From`, `To`, `Subject`, and `Date` headers plus its body text; attachment data is never supplied by `imap-api.load-by-date-at`.
3. Opens a multiline `text-input-api.prompt` initialized to an advertisement-confidence classification instruction.
4. On confirmation, appends each displayed header and body text in turn to that instruction and sends each resulting text through `ollama-api.generate` with a JSON Schema that requires `advertisementConfidencePercent` as an integer from 0 through 100 and a temperature of zero.
5. Parses each returned JSON response and writes the trial number, message number, submitted instruction, and confidence value or response failure to the application console log.
6. Reopens the multiline prompt with the last confirmed instruction prefilled and repeats until cancellation.

## Test expectations

The display contains the same sample of up to ten earliest-dated loaded messages throughout a sequence of submitted prompts. Each confirmed prompt results in one Ollama generation request per loaded message containing the instruction, the relevant message content, and the required confidence JSON Schema; each valid logged result identifies its trial, message, instruction, and parsed percentage. The prior instruction is prefilled on the next prompt, and cancellation requests orderly shutdown.
