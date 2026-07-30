# Privacy-first public-link playtest design

Date: 2026-07-30  
Status: Approved design pending written-spec review  
Canonical owner: `kiku-jw/wobble-stack#4`  
Document class: temporary task artifact

This document replaces the earlier per-participant-code design. Issue #4 owns
current task state. After the cohort decision, this document is superseded by
an immutable experiment receipt containing the frozen build, observed results,
limitations, decision, and next test.

## Known evidence

Three friends have already completed the current game. They described it as
interesting and asked for something more. This is useful qualitative evidence
that the existing content can be completed, but it does not answer whether the
loop earns repeated rounds or a later return.

Those three sessions happened before instrumentation and are not counted in the
new telemetry cohort. The game link has also already appeared in the owner's
Telegram channel. Some later participants may therefore have seen or played an
earlier build; the experiment receipt must state that exposure limitation.

## Outcome

Publish one replaceable link in the owner's Telegram channel and, if needed,
other owned or appropriate resources. Measure whether the unchanged gameplay
loop earns repeated rounds and a return on another local calendar day before
adding routes, progression, monetization, advertising, localization, or paid
acquisition.

The primary cohort is the first 20 anonymous browser installations that start a
round on the frozen instrumented build. A browser installation is only a proxy
for a person: clearing storage, changing browsers, or changing devices can
create another identity. The result must not be described as 20 verified unique
people.

## Product gates

The thresholds are practical continuation filters, not market benchmarks:

| Signal | Continue threshold |
| --- | ---: |
| Primary participants who start at least five rounds | 12 of 20 |
| Primary participants who start a round on another local calendar day | 6 of 20 |

Retry after a loss, round duration, route progress, badges, jumps, obstacle
hits, and obstacle clears are diagnostic signals. They explain the two primary
gates but do not become extra pass/fail targets after results are known.

Decision rules:

- If Repeat and Return pass, select one substantial addition from observed
  drop-off and qualitative feedback.
- If Repeat passes but Return fails, test one lightweight reason to return,
  such as visible session progress or a continuing challenge.
- If Repeat fails, revise the first 30–60 seconds, control clarity, or the
  consequence/recovery loop. Do not add more routes.
- If ingestion, launch, Telegram handoff, input, or storage failures affect more
  than 20% of observable participants, repair the test and rerun it before
  judging the game.
- If fewer than 8 of 20 start five rounds or fewer than 3 of 20 return on
  another day, stop content expansion until a new core-loop hypothesis exists.

## Recruitment and cohort boundary

The shared link uses a non-personal cohort and source label:

```text
https://kiku-jw.github.io/wobble-stack/?playtest=TG1&source=telegram
```

Links posted elsewhere use the same cohort and a different allow-listed source,
for example `source=website`. Source is the invitation label, not a reliable
referrer or identity signal.

Rules:

- Enrollment begins only after the instrumented build and collector are live.
- The first 20 browser IDs whose valid `round_started` event reaches the
  collector form the locked primary cohort.
- Later participants remain visible as exploratory traffic but never replace a
  primary participant based on their result.
- If the channel does not reach 20, the same frozen build and cohort may be
  posted on other resources until 20 participants start.
- Each primary participant receives a 48-hour observation window after their
  first round. No reminder is posted during that window.
- Gameplay and event semantics remain frozen while enrollment is open.

The old channel link may be replaced or followed by a new post. Previous
uninstrumented visits are not reconstructed or invented.

## Consent and identity

Ordinary visitors receive the existing game and create no telemetry identity.
Playtest mode activates only for an allow-listed `playtest` query value.

Before the first instrumented round, the test link shows a short notice:

> This test version sends anonymous round summaries to help improve the game.
> No name, Telegram account, messages, or precise controls are collected.

The visitor can choose:

- `Play and join the test`, which creates a random browser ID and enables
  telemetry; or
- `Play privately`, which starts the ordinary game without telemetry.

The ID is generated in the browser and stored locally. The application does not
collect or store names, usernames, Telegram IDs, email addresses, phone
numbers, messages, precise pointer movement, user-agent strings, fingerprints,
precise location, or IP addresses in its database. Cloudflare necessarily
processes network metadata to deliver the request, but the Worker does not add
it to the experiment record.

## Events

The game sends semantic summaries, never an input stream.

### `round_started`

Sent once when gameplay actually starts:

```text
eventId
participantId
cohort
build
source
launchContext: telegram | external
localDay
clientSequence
routeId
```

### `round_finished`

Sent once on collapse results or route completion:

```text
eventId
participantId
cohort
build
source
localDay
clientSequence
routeId
outcome: loss | completed
durationSeconds
progressPercent
badges
jumps
obstaclesHit
obstaclesCleared
```

Values are integer, bounded summaries. `progressPercent` is rounded to a whole
percent and duration is capped. No coordinates, frame samples, physics state,
or exact control timing leave the browser.

### `retry_clicked`

Sent only when the visitor presses Retry from the loss result:

```text
eventId
participantId
cohort
build
source
localDay
clientSequence
routeId
```

Start, Replay after completion, Next road, route selection, reload, and debug
controls never increment Retry. Every real gameplay start increments the round
and route-attempt counts exactly once.

The collector assigns its own receipt timestamp. `localDay` is used only to
classify a different-day return and contains no time or timezone.

## Browser recorder

A small pure module owns query validation, identity creation, event
construction, bounds, retry classification, and deterministic tests. It does
not access Matter.js, the DOM, storage, clipboard, or the network.

A browser adapter owns:

- the scoped random ID, consent, sequence, and bounded pending-event outbox;
- `fetch` delivery after meaningful transitions;
- retry with deduplication when the collector is temporarily unavailable;
- a strict maximum of 50 queued events;
- deletion of only the current playtest keys.

If storage or the network is unavailable, the game remains playable. Pending
telemetry never blocks input, results, Retry, or navigation.

Existing UI actions pass explicit semantic reasons before their shared
`startRun` transition. The recorder cannot change wind, physics, scoring,
routes, unlocks, input, music, or result timing.

The existing Telegram external-browser handoff must preserve the full playtest
query. No visit or round is counted in the embedded browser if the player opens
the link externally before starting.

## Collector

A first-party Cloudflare Worker exposes:

- `POST /v1/events` for a small event batch;
- `POST /v1/delete` to delete the current participant ID from the active
  cohort; and
- `GET /health` without experiment data.

Cloudflare D1 stores the validated events. There is no public analytics
dashboard and no third-party analytics SDK. The owner queries aggregate results
privately through authenticated Cloudflare tooling.

Collector controls:

- accept requests only from the published game origin;
- allow-list schema version, cohort, build, source, event type, and route ID;
- reject malformed or oversized bodies;
- bound strings and numeric values;
- deduplicate the globally random `eventId`;
- rate-limit abusive clients;
- use parameterized D1 queries;
- return generic errors without request-body logging;
- keep secrets only in Cloudflare configuration, never in Git.

The delete endpoint removes server events for the supplied unguessable local
participant ID and clears local telemetry state after success. It cannot delete
ordinary game progress or music preferences.

## Storage and retention

Raw events are retained for 30 days. A daily Worker scheduled task removes
older rows. At cohort close, the owner keeps only an aggregate experiment
receipt and deletes the remaining raw cohort events after verifying the
receipt.

The receipt records:

- build and collection window;
- primary and exploratory counts;
- Repeat and Return results;
- Retry and round diagnostics;
- source mix and technical-failure count;
- prior-exposure and browser-identity limitations;
- qualitative feedback;
- the decision and next experiment.

## Verification and rollout

Automated tests cover:

- inactive ordinary mode and explicit consent;
- anonymous ID and event-schema validation;
- exact Start, Finish, Retry, Replay, and Next-road semantics;
- counter bounds and local-day classification;
- outbox recovery and deduplication;
- scoped deletion;
- Worker origin, payload, allow-list, rate-limit, and D1 behavior;
- aggregation that locks the first 20 starters without replacement.

Browser checks cover:

- 320 × 700, 390 × 844, and desktop layout;
- Telegram notice and full-query external handoff;
- consent and private-play paths;
- steering, jump, meaningful obstacle hits, loss, Retry, completion, pause, and
  music regression;
- offline play and later outbox delivery;
- zero unexpected third-party requests and zero console errors.

Before cohort enrollment, excluded pilot IDs verify the live Worker, D1,
deletion, mobile layout, and Telegram handoff. The three earlier friends do not
need to repeat the gameplay test merely to validate instrumentation.

Release requires a fresh independent verifier, safe `origin/main` publication,
the existing Pages workflow, Cloudflare deployment explicitly authorized by
the owner, and a live smoke using an excluded pilot cohort.

## Non-goals

- No accounts, fingerprinting, precise interaction replay, heatmaps, advertising
  identifiers, third-party analytics SDK, public dashboard, or automatic
  contact with participants.
- No ads, Remove Ads, IAP, shop, currency, leaderboard, skins, new routes, or
  gameplay A/B split during this cohort.
- No claim that a browser ID is a verified person or that 20 friendly
  participants predict market-scale retention or economics.
- No Unity-client changes.
