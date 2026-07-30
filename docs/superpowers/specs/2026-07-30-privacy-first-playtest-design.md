# Privacy-first repeat-cohort playtest design

Date: 2026-07-30  
Status: Approved design pending written-spec review  
Canonical owner: `kiku-jw/wobble-stack#4`  
Document class: temporary task artifact

This document translates the approved two-week product experiment into a
maintainable design. Issue #4 owns current task state. After the cohort
decision, this document is superseded by a dated experiment receipt containing
the build, cohort, results, decision, and next test.

## Outcome

Determine whether the current browser loop earns repeated rounds and an
unprompted return on a later local calendar day before adding more content,
meta-progression, advertising, monetization, localization, or paid
acquisition.

The experiment uses one unchanged gameplay build for all 20 primary
participants. It does not split the cohort into an underpowered A/B test.

## Product gates

The cohort is a practical continuation filter, not an industry benchmark:

| Signal | Continue threshold |
| --- | ---: |
| Participants who start at least five rounds | 12 of 20 |
| Participants who play on another local calendar day before the reporting reminder | 6 of 20 |
| Participants who sent or wanted to show the game to another person | 5 of 20 |
| Participants who start their first round without assistance | 16 of 20 |

Decision rules:

- If Repeat and Return pass, test one substantial loop variant selected from
  observed drop-off.
- If Repeat passes but Return fails, test one lightweight reason to return,
  such as continuing a route or visible session progress.
- If Repeat fails, revise the first 30–60 seconds, control clarity, or
  consequence/recovery loop. Do not add routes or meta.
- If launch, Telegram, input, or result-export failures affect more than 20% of
  participants, fix distribution and rerun the same experiment before judging
  the game.
- If fewer than 8 of 20 start five rounds or fewer than 3 of 20 return on
  another day, stop content expansion until a new core-loop hypothesis exists.

## Activation and isolation

Ordinary players receive the current game with no playtest UI, storage, or
behavior changes.

Playtest mode activates only through an explicit URL:

```text
?playtest=P07&cohort=R1
```

Telegram invitations may combine the existing handoff query:

```text
?from=telegram&playtest=P07&cohort=R1
```

Rules:

- `playtest` is an anonymous participant code matching
  `^[A-Z0-9-]{1,12}$`.
- `cohort` is a non-personal experiment label with the same format.
- Names, usernames, phone numbers, email addresses, account identifiers, free
  text, IP addresses, user-agent strings, screen fingerprints, and precise
  location are never collected.
- Invalid or missing codes keep the ordinary game active and create no
  playtest storage.
- The playtest indicator states that results stay on the device until the
  participant deliberately copies them.

## Local data model

One versioned localStorage record is scoped by cohort, public build label, and
participant code:

```text
wobble-stack-playtest:v1:R1:R1-01:P07
```

The record contains only:

```text
version
participantCode
cohort
build
firstSeenAt
lastSeenAt
lastActiveAt
playDates[]
sessionCount
roundsStarted
roundsFinished
losses
routeCompletions
retries
routeAttemptsById
routeCompletionsById
firstRoundStartedAt
firstLossAt
firstRetryAt
launchContext: telegram | external
```

Timing rules:

- Timestamps remain local and are not transmitted automatically.
- Opening or reloading the page does not count as a play session.
- A new session starts on `startRun` when there is no earlier played session,
  after at least 30 minutes without gameplay activity, or on a different local
  calendar day.
- `playDates` stores only dates on which a round started. Merely opening the
  reporting panel on another day cannot create a Return.
- A round starts only when gameplay starts.
- A round finishes on either collapse results or route completion.
- Retry increments only from a result state, not from page reload or route
  selection.
- Route attempts and completions use existing route IDs.
- `launchContext=telegram` means the invitation used `from=telegram` or a known
  Telegram bridge was detected. It is a coarse distribution label, not proof
  that the current runtime is still an embedded browser.
- `build` is a short public compile-time cohort label such as `R1-01`. It is
  part of the storage key so results from a changed build cannot silently mix
  with the frozen cohort.
- The stored record is updated at meaningful transitions, not every animation
  frame or pointer event.

## Components and boundaries

### Pure playtest logic

A small module owns:

- query parsing and code validation;
- default record creation and schema validation;
- session/date classification;
- transition reducers for start, loss, completion, and Retry;
- deterministic human-readable summary formatting.

It does not access the DOM, Matter.js, the network, or browser clipboard APIs.

### Browser storage adapter

A narrow adapter reads and writes the one versioned localStorage key. It:

- handles unavailable, blocked, malformed, or quota-limited storage without
  crashing the game;
- never enumerates unrelated localStorage keys;
- never migrates or uploads data;
- exposes a deliberate reset for the current cohort/code only.

### Game integration

Existing game transitions call the playtest recorder:

- page load to initialize or read the scoped record without counting a session;
- initial Start, result-state Retry, finished-route Replay, and Next road through
  explicit semantic handlers before their shared `startRun` transition;
- failure results;
- route completion;

The recorder does not infer intent from `startRun` itself. Only the
result-overlay Retry handler increments `retries`; Start, Replay, Next road,
route selection, reload, and debug controls never do. Every successful gameplay
start still increments the round and route-attempt counters exactly once.

The integration is observational. It cannot change wind, physics, scoring,
routes, unlocks, input, music, or result timing.

### Playtest UI

Only playtest mode adds a compact panel to the start overlay:

- `PLAYTEST P07 · R1`;
- a one-line local-only privacy statement;
- `Copy test result`;
- `Reset test result`.

Copy uses the Clipboard API after a user click. If clipboard access is
unavailable, a selectable text fallback is shown. Reset requires a second
confirmation click and affects only the current playtest key.

The panel must fit at 320 × 700 without hiding route selection or Start.

## Shared result format

The copied result contains no exact dates or raw timestamps:

```text
WOBBLE PLAYTEST v1
participant=P07
cohort=R1
build=R1-01
context=telegram
sessions=3
play_days=2
returned_other_day=yes
rounds_started=7
rounds_finished=6
losses=5
retries=5
route_completions=1
routes=orchard:7/1,cloud:0/0,windmill:0/0
seconds_to_first_loss=42
seconds_to_first_retry=51
storage=ok
```

If storage is unavailable, the game remains playable and the panel produces an
honest `storage=unavailable` result rather than invented zeros.

## Cohort procedure

### Technical pilot

Three people outside the primary cohort verify:

- Telegram invitation and external-browser handoff;
- playtest activation and anonymous code;
- touch steering, jump, obstacle hit/clear, and Retry;
- persistence across reload and a simulated later date;
- copy fallback and scoped reset.

Technical-pilot data is excluded from the 20-person result.

### Primary cohort

- Send all 20 participants the same gameplay build with unique anonymous codes.
- Instruction: “Open the game and play as much as you want.”
- Do not mention the five-round target.
- Do not coach after launch unless recording a technical failure.
- Send no play reminder during the first 24 hours.
- After 48 hours, ask participants to copy the result and answer:
  1. Why did you press Retry, or why did you not?
  2. Why did you stop playing?
  3. Did you send or want to show the game to someone else?

The 48-hour message requests reporting; a return that happens only after that
message does not count as an unprompted different-day return.

## Verification

Automated checks cover:

- query validation and inactive ordinary mode;
- schema validation and corrupt-storage recovery;
- 30-minute and different-date session boundaries;
- exact event counter semantics;
- no Retry count from reload or route selection;
- deterministic summary formatting without forbidden fields;
- scoped reset;
- unavailable-storage behavior.

Production browser checks cover:

- ordinary launch creates no playtest key or UI;
- playtest launch persists across reload;
- start, loss, Retry, and route completion update once;
- Telegram and external links report the correct coarse context;
- copy success, fallback, and reset confirmation;
- 320 × 700, 390 × 844, and desktop layout;
- zero unexpected network requests;
- zero console errors;
- current steering, jump, obstacles, pause, music, and Retry remain intact.

Release requires a fresh read-only verifier, safe `origin/main` publication,
the existing Pages workflow, and a public smoke using anonymous pilot codes.

## Non-goals

- No backend, analytics SDK, fingerprinting, account, remote database, webhook,
  dashboard, or automatic send.
- No paid traffic, ads, Remove Ads, IAP, shop, currency, leaderboard, skins, or
  new route content.
- No gameplay A/B split inside the 20-person cohort.
- No Unity-client changes in this experiment.
- No claim that the cohort thresholds predict market-scale retention or
  economics.

## Failure handling and deletion

- Technical failures are separated from gameplay rejection.
- A participant may reset the current local record at any time.
- The experiment owner keeps only submitted text summaries and the cohort
  decision; no browser storage can be remotely deleted because it never leaves
  the participant device automatically.
- After the cohort decision, supersede this design with an immutable experiment
  receipt and remove any temporary participant-code assignment sheet according
  to its own retention decision.
