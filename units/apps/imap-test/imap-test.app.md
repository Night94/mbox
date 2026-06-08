---
mbox_unit: 1
unit: imap-test
type: app
version: 21
uses:
  app: 6
  runtime: 1
  mbox-dotnet: 3
  imap-test-main: 5
  imap: 4
  mail-classifier: 3
  ollama: 3
  display: 3
  imap-api: 2
  mail-classifier-api: 1
  display-api: 2
  ollama-api: 3
---

# imap-test

Loads INBOX messages oldest timestamp first for the calendar year of the first loaded message, classifies each in-year message through `mail-classifier`, and moves each into the destination folder the classifier returns. The displayed report updates while processing and shows one move-result line for every processed message, including expected non-matches.

## Definition

```yaml
entryBox: imap-test-main
boxes:
  - imap-test-main
  - imap
  - mail-classifier
  - ollama
  - display
bindings:
  - consumer: imap-test-main
    interface: imap-api
    operations: [count-messages, load-by-date-at, move-message]
    provider: imap
  - consumer: imap-test-main
    interface: mail-classifier-api
    operations: [classify]
    provider: mail-classifier
  - consumer: imap-test-main
    interface: display-api
    operations: [show-window, show-string, use-multitext]
    provider: display
  - consumer: mail-classifier
    interface: ollama-api
    operations: [generate]
    provider: ollama
externalProviders: []
exposes: []
configuration:
  imap.host: "imap.example.com"
  imap.user: "user@example.com"
  imap.pwd: "<imap-password>"
  ollama.baseUrl: "http://localhost:11434"
  ollama.model: "llama3:latest"
  tts.speakerId: 1
  tts.speed: 1.0
  Classifier.Rules:
    - "MATCH INBOX.sytech to micha@sytechcorp.com"
    - "MATCH INBOX.autosort.bank from donotreply@email.nationstarmail.com"
    - "MATCH INBOX.autosort.bank from donotreply@interactivebrokers.com"
    - "MATCH INBOX.autosort.bank from erivera@citylendinginc.com"
    - "MATCH INBOX.autosort.bank from info@secretdeals-online.de"
    - "MATCH INBOX.autosort.bank from kundeninformation@ihre.dkb.de"
    - "MATCH INBOX.autosort.bank from no-reply@simplifyem.com"
    - "MATCH INBOX.autosort.bank from noreply@entropay.com"
    - "MATCH INBOX.autosort.bank from noresponse@interactivebrokers.com"
    - "MATCH INBOX.autosort.bees from kim@beeculture.com"
    - "MATCH INBOX.autosort.bees from nvba35@wildapricot.org"
    - "MATCH INBOX.autosort.booking from email.campaign@sg.booking.com"
    - 'MATCH "INBOX.autosort.car related" from allstate@billing01.email-allstate.com'
    - 'MATCH "INBOX.autosort.car related" from allstate@marketing01.email-allstate.com'
    - 'MATCH "INBOX.autosort.car related" from allstate@service01.email-allstate.com'
    - 'MATCH "INBOX.autosort.car related" from customerservice@fordtradeassistance.us'
    - 'MATCH "INBOX.autosort.car related" from donotreply@routeone.com'
    - 'MATCH "INBOX.autosort.car related" from ffzrigrnkb@mail2world.com'
    - 'MATCH "INBOX.autosort.car related" from tdeserio@webmail.cdkcrm.com'
    - 'MATCH "INBOX.autosort.car related" from vzimbro1@allstate.com'
    - "MATCH INBOX.autosort.domain from donotreply@godaddy.com"
    - "MATCH INBOX.autosort.domain from notifications-noreply@lunarpages.com"
    - "MATCH INBOX.autosort.domain from renewals@godaddy.com"
    - "MATCH INBOX.autosort.dse from gus@mjbraun.com"
    - "MATCH INBOX.autosort.ezpass from noreply@ezpassva.com"
    - "MATCH INBOX.autosort.facebook from @facebookmail.com"
    - "MATCH INBOX.autosort.facebook from notification+kjdmiw_jipw_@pages.facebookmail.com"
    - "MATCH INBOX.autosort.geocaching from 44e62651-5ab4-45e1-9cf4-51088376cc0a@mc.geocaching.com"
    - "MATCH INBOX.autosort.geocaching from noreply@geocaching.com"
    - "MATCH INBOX.autosort.gov from donotreplyenotify@dmv.virginia.gov"
    - "MATCH INBOX.autosort.gov from no-reply@cbp.dhs.gov"
    - "MATCH INBOX.autosort.gov from no-reply@ssa.gov"
    - "MATCH INBOX.autosort.gov from noreply@opusinspection.com"
    - "MATCH INBOX.autosort.junk_political from elizabeth@abigailspanberger.com"
    - "MATCH INBOX.autosort.junk_political from emma@abigailspanberger.com"
    - "MATCH INBOX.autosort.junk_political from info2@abigailspanberger.com"
    - "MATCH INBOX.autosort.junk_political from khizr@abigailspanberger.com"
    - "MATCH INBOX.autosort.linkedin from invitations@linkedin.com"
    - "MATCH INBOX.autosort.linkedin from messages-noreply@linkedin.com"
    - "MATCH INBOX.autosort.nextdoor from no-reply@rs.email.nextdoor.com"
    - "MATCH INBOX.autosort.nextdoor from noreply@ms.email.nextdoor.com"
    - "MATCH INBOX.autosort.nextdoor from reply@rs.email.nextdoor.com"
    - "MATCH INBOX.autosort.Parties from evite@mailva.evite.com"
    - "MATCH INBOX.autosort.personal from dlaufer@gmx.net"
    - "MATCH INBOX.autosort.personal from fischer.nicola@icloud.com"
    - "MATCH INBOX.autosort.personal from pattywoltman@gmail.com"
    - "MATCH INBOX.autosort.personal from vollmann.dirk@gmail.com"
    - "MATCH INBOX.autosort.properties from noreply@republicservices.com"
    - "MATCH INBOX.autosort.properties from notify@buildinglink.com"
    - "MATCH INBOX.autosort.properties from support@screentimelabs.com"
    - "MATCH INBOX.autosort.purchases from donotreply@showclix.com"
    - "MATCH INBOX.autosort.purchases from homedepotreceipt@homedepot.com"
    - "MATCH INBOX.autosort.purchases from hulu@hulumail.com"
    - "MATCH INBOX.autosort.purchases from info@tonneaucoverswarehouse.com"
    - "MATCH INBOX.autosort.purchases from newsletter@notifications.lumosity.com"
    - "MATCH INBOX.autosort.purchases from no-reply@e.siriusxm.com"
    - "MATCH INBOX.autosort.purchases from no-reply@net--flix-billingassetss1.com.uji.es"
    - "MATCH INBOX.autosort.purchases from no-reply@orders.searshomeservices.io"
    - "MATCH INBOX.autosort.purchases from no-reply@philips.com"
    - "MATCH INBOX.autosort.purchases from noreply@homedepot.com"
    - "MATCH INBOX.autosort.purchases from service@paypal.de"
    - "MATCH INBOX.autosort.purchases from support@oculus.com"
    - "MATCH INBOX.autosort.purchases from vegetationmgmt@novec.com"
    - "MATCH INBOX.autosort.purchases from verizon-notification@verizon.com"
    - "MATCH INBOX.autosort.purchases from webcontent@novec.com"
    - 'MATCH "INBOX.autosort.rental cars" from ebillingde@europcar.de'
    - "MATCH INBOX.autosort.rideshare from no-reply@lyftmail.com"
    - "MATCH INBOX.autosort.rideshare from uber.us@uber.com"
    - "MATCH INBOX.autosort.roblox from support-en@roblox.com"
    - "MATCH INBOX.autosort.school from autoreply@bloomz.net"
    - "MATCH INBOX.autosort.school from info@scoutbook.com"
    - "MATCH INBOX.autosort.school from no-reply@t.mail.coursera.org"
    - "MATCH INBOX.autosort.school from tabarcus@fcps.edu"
    - "MATCH INBOX.autosort.school from ulricjr@hotmail.com"
    - "MATCH INBOX.autosort.steam from noreply@steampowered.com"
    - "MATCH INBOX.autosort.taxes from do_not_reply@intuit.com"
    - "MATCH INBOX.autosort.taxes from noreply@velocitypayment.com"
    - "MATCH INBOX.autosort.taxes from sccefile@scc.virginia.gov"
    - "MATCH INBOX.autosort.taxes from taxman@fileyourtaxes.com"
    - "MATCH INBOX.autosort.taxes from turbotax@e.turbotax.intuit.com"
    - "MATCH INBOX.autosort.taxes from turbotax@intuit.com"
    - "MATCH INBOX.autosort.travel from flightupdate@your.lufthansa-group.com"
    - "MATCH INBOX.autosort.travel from ihgrewardsclub@sm.ihg.com"
    - "MATCH INBOX.autosort.travel from ihgrewardsclub@sv.ihg.com"
    - "MATCH INBOX.autosort.travel from ihgrewardsclubstatement@sm.ihg.com"
    - "MATCH INBOX.autosort.travel from info@mail.hotels.com"
    - "MATCH INBOX.autosort.travel from jetblueairways@email.jetblue.com"
    - "MATCH INBOX.autosort.travel from milesmore@mailing.milesandmore.com"
    - "MATCH INBOX.autosort.travel from no-reply@flysas.com"
    - "MATCH INBOX.autosort.travel from no-reply@gotobus.com"
    - "MATCH INBOX.autosort.travel from noreply@flysas.net"
    - "MATCH INBOX.autosort.travel from noreply@lufthansa.com"
    - "MATCH INBOX.autosort.travel from online@booking-lufthansa.com"
    - "MATCH INBOX.autosort.travel from qbtseventy@basincoop.com"
    - "MATCH INBOX.autosort.travel from sasupgrade@flysas.com"
    - "MATCH INBOX.autosort.travel from service@your.lufthansa-group.com"
    - "MATCH INBOX.autosort.travel from starwoodpreferredguest@member.starwoodhotelsemail.com"
    - "MATCH INBOX.autosort.travel from support@mailer.orbitz.com"
    - "ASK INBOX.school This email concerns school, education, students, teachers, classes, coursework, enrollment, tuition, parent-school communication, or school activities."
    - "ASK INBOX.insurance This email concerns insurance policies, insurance coverage, insurance premiums, claims, benefits statements, insurance renewal, an insurance provider, or communications from an insurer."
    - "ASK INBOX.medical This email concerns healthcare, doctors, dentists, hospitals, clinics, pharmacies, prescriptions, medical appointments, test results, patient portals, or medical billing."
```

## Purpose

Composes a working mailbox triage pipeline that exercises three common boxes plus a rule-driven classifier with deterministic and `ASK`-style rules.

Rules whose destination is under `INBOX.autosort` are deterministic rules derived from messages placed in the corresponding `INBOX.autosort.<category>.samples` folder. They use explicit sender addresses by default and may use a sender domain only after more than four sampled sender addresses at that domain map to the same destination. They may be replaced as a group when the sample set is refreshed; a sample folder with no messages produces no rule.

## Startup behavior

The framework dispatches `run` to `imap-test-main` after `init` completes. The entry box runs the load-classify-move pass over INBOX in ascending message `Date` timestamp order for the calendar year of the first successfully loaded message, stopping before it classifies or moves a message from a later year. It refreshes the multiline display during processing and then requests shutdown after a brief reading delay.

## Failure behavior

`no-matching-rule` is treated as an expected non-match and reported as `not moved`. Other operation failures are reported on the display after the affected message's `not moved` line. A failure that prevents continuing the iteration terminates `run` with an exception and triggers application shutdown via the runtime's fatal-`run` handling.

## Test expectations

Against a reachable IMAP account and a reachable Ollama endpoint, the application incrementally displays each processed message in ascending message `Date` timestamp order with sender, recipient, and move result, processes messages only through the end of the first loaded message's calendar year, includes genuine errors, and terminates cleanly after the reading delay.
