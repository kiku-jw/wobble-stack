# Public-Link Privacy-First Playtest Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish one opt-in playtest link that sends bounded anonymous round summaries to a first-party Cloudflare Worker + D1 collector, preserves private and ordinary play, and locks the first 20 browser IDs as the primary cohort.

**Architecture:** The Vite game gets a pure event module and a browser-only storage/delivery adapter. Game transitions emit semantic summaries without affecting physics. A separate Cloudflare Worker validates and deduplicates batches, stores them in D1, assigns the first 20 starters to fixed cohort slots, supports participant deletion and 30-day cleanup, and exposes no public report.

**Tech Stack:** Vanilla JavaScript, Vite, Node.js `node:test`, Matter.js, Cloudflare Workers, D1, Wrangler, GitHub Pages.

---

## Scope and file map

The browser recorder and collector are two dependent halves of one experiment:
neither is useful without the other, so they stay in one implementation plan.

- Create `src/playtest-logic.js`: pure query parsing, event construction, bounds,
  date bucketing, and constants.
- Create `src/playtest-client.js`: local consent/identity/outbox state and
  network delivery.
- Create `test/playtest-logic.test.js`: pure event semantics.
- Create `test/playtest-client.test.js`: storage, retry, deletion, and failure
  isolation.
- Modify `src/game.js`: explicit event hooks and per-round counters.
- Modify `index.html`: compact consent overlay and telemetry controls.
- Modify `src/style.css`: responsive consent/status treatment.
- Create `worker/src/validation.js`: strict untrusted-input validation.
- Create `worker/src/index.js`: Worker HTTP, CORS, rate limit, D1 writes,
  deletion, and scheduled cleanup.
- Create `worker/migrations/0001_playtest.sql`: immutable event and cohort-slot
  schema.
- Create `worker/queries/cohort-summary.sql`: private Repeat/Return diagnostics.
- Create `worker/test/validation.test.js`: validation boundary tests.
- Create `worker/test/worker.test.js`: request, D1, deduplication, deletion, and
  cleanup tests with binding fakes.
- Create `worker/wrangler.jsonc`: Worker, D1, rate-limit, privacy, and cron
  configuration.
- Modify `package.json` and `pnpm-lock.yaml`: Wrangler and Worker checks.
- Modify `.github/workflows/deploy-pages.yml`: inject the public collector URL
  from a non-secret repository variable.
- Modify `docs/TEST_PLAN.md`, `docs/STATUS.md`, and
  `docs/IMPLEMENTATION_NOTES.md`: durable operator and release truth.
- Supersede
  `docs/superpowers/specs/2026-07-30-privacy-first-playtest-design.md` after the
  cohort with a later immutable receipt; do not delete it during implementation.

### Task 1: Pure browser playtest events

**Files:**
- Create: `src/playtest-logic.js`
- Create: `test/playtest-logic.test.js`

- [ ] **Step 1: Write failing query and event tests**

Create tests that import the not-yet-existing module:

```js
import test from "node:test";
import assert from "node:assert/strict";
import {
  PLAYTEST_BUILD,
  createFinishedEvent,
  createRetryEvent,
  createStartedEvent,
  getLocalDay,
  parsePlaytestConfig,
} from "../src/playtest-logic.js";

const base = {
  participantId: "018f47d4-6f5c-7f67-91cf-93e12bf2b6f1",
  cohort: "TG1",
  source: "telegram",
  launchContext: "external",
  localDay: "2026-07-30",
  clientSequence: 1,
  routeId: "orchard",
};

test("playtest activation is explicit and allow-listed", () => {
  assert.equal(parsePlaytestConfig(""), null);
  assert.equal(parsePlaytestConfig("?playtest=BAD&source=telegram"), null);
  assert.deepEqual(
    parsePlaytestConfig("?from=telegram&playtest=TG1&source=telegram"),
    { cohort: "TG1", source: "telegram", build: PLAYTEST_BUILD },
  );
  assert.deepEqual(
    parsePlaytestConfig("?playtest=TG1-PILOT&source=pilot"),
    { cohort: "TG1-PILOT", source: "pilot", build: PLAYTEST_BUILD },
  );
});

test("local day contains no time or timezone", () => {
  assert.equal(getLocalDay(new Date(2026, 6, 30, 23, 59)), "2026-07-30");
});

test("round summaries are bounded and semantic", () => {
  const started = createStartedEvent(base, () => "event-start");
  const finished = createFinishedEvent({
    ...base,
    clientSequence: 2,
    durationSeconds: 999999,
    progressPercent: 42.7,
    badges: 99,
    jumps: 12,
    obstaclesHit: 3,
    obstaclesCleared: 2,
    outcome: "loss",
  }, () => "event-finish");
  const retry = createRetryEvent(
    { ...base, clientSequence: 3 },
    () => "event-retry",
  );

  assert.equal(started.type, "round_started");
  assert.equal(finished.durationSeconds, 1800);
  assert.equal(finished.progressPercent, 43);
  assert.equal(finished.badges, 20);
  assert.equal(retry.type, "retry_clicked");
  assert.equal("coordinates" in finished, false);
});
```

- [ ] **Step 2: Run the focused test and confirm the missing-module failure**

Run:

```bash
node --test test/playtest-logic.test.js
```

Expected: FAIL with `ERR_MODULE_NOT_FOUND` for `src/playtest-logic.js`.

- [ ] **Step 3: Implement the pure module**

Create the following public contract:

```js
export const PLAYTEST_VERSION = 1;
export const PLAYTEST_BUILD = "TG1-01";

const COHORTS = new Set(["TG1", "TG1-PILOT"]);
const SOURCES = new Set(["telegram", "website", "pilot"]);
const ROUTES = new Set(["orchard", "cloud", "windmill"]);
const OUTCOMES = new Set(["loss", "completed"]);

function boundedInteger(value, minimum, maximum) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) return minimum;
  return Math.min(maximum, Math.max(minimum, Math.round(numeric)));
}

function requireBase(base) {
  if (!base || !COHORTS.has(base.cohort) || !SOURCES.has(base.source)) {
    throw new TypeError("Invalid playtest event context");
  }
  if (!ROUTES.has(base.routeId)) throw new TypeError("Invalid route");
  if (!/^[0-9a-f-]{36}$/i.test(base.participantId)) {
    throw new TypeError("Invalid participant ID");
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(base.localDay)) {
    throw new TypeError("Invalid local day");
  }
  return {
    version: PLAYTEST_VERSION,
    participantId: base.participantId,
    cohort: base.cohort,
    build: PLAYTEST_BUILD,
    source: base.source,
    localDay: base.localDay,
    clientSequence: boundedInteger(base.clientSequence, 1, 1000000),
    routeId: base.routeId,
  };
}

function eventId(randomUUID) {
  const value = randomUUID();
  if (!/^[0-9a-f-]{36}$/i.test(value) && !/^event-[a-z-]+$/i.test(value)) {
    throw new TypeError("Invalid event ID");
  }
  return value;
}

export function parsePlaytestConfig(search) {
  const params = new URLSearchParams(search);
  const cohort = params.get("playtest") || "";
  const source = params.get("source") || "";
  return COHORTS.has(cohort) && SOURCES.has(source)
    ? { cohort, source, build: PLAYTEST_BUILD }
    : null;
}

export function getLocalDay(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function createStartedEvent(base, randomUUID = crypto.randomUUID.bind(crypto)) {
  return {
    ...requireBase(base),
    eventId: eventId(randomUUID),
    type: "round_started",
    launchContext: base.launchContext === "telegram" ? "telegram" : "external",
  };
}

export function createFinishedEvent(base, randomUUID = crypto.randomUUID.bind(crypto)) {
  if (!OUTCOMES.has(base.outcome)) throw new TypeError("Invalid outcome");
  return {
    ...requireBase(base),
    eventId: eventId(randomUUID),
    type: "round_finished",
    outcome: base.outcome,
    durationSeconds: boundedInteger(base.durationSeconds, 0, 1800),
    progressPercent: boundedInteger(base.progressPercent, 0, 100),
    badges: boundedInteger(base.badges, 0, 20),
    jumps: boundedInteger(base.jumps, 0, 1000),
    obstaclesHit: boundedInteger(base.obstaclesHit, 0, 20),
    obstaclesCleared: boundedInteger(base.obstaclesCleared, 0, 20),
  };
}

export function createRetryEvent(base, randomUUID = crypto.randomUUID.bind(crypto)) {
  return {
    ...requireBase(base),
    eventId: eventId(randomUUID),
    type: "retry_clicked",
  };
}
```

The permissive `event-*` IDs exist only for deterministic tests; production
passes `crypto.randomUUID`.

- [ ] **Step 4: Run the focused and full test suites**

Run:

```bash
node --test test/playtest-logic.test.js
pnpm test
```

Expected: the new tests and the existing 23 tests PASS.

- [ ] **Step 5: Commit the pure event contract**

```bash
git add src/playtest-logic.js test/playtest-logic.test.js
git commit -m "feat: define anonymous playtest events"
```

### Task 2: Local consent, identity, outbox, and deletion

**Files:**
- Create: `src/playtest-client.js`
- Create: `test/playtest-client.test.js`

- [ ] **Step 1: Write failing client tests**

Use a `Map`-backed storage fake and a fetch spy. Cover:

```js
test("inactive mode creates no keys or requests", async () => {
  const harness = createHarness({ config: null });
  assert.equal(harness.client.status().mode, "inactive");
  assert.equal(harness.storage.size, 0);
  assert.equal(harness.requests.length, 0);
});

test("private choice stores no participant identity", async () => {
  const harness = createHarness();
  harness.client.choosePrivate();
  assert.equal(harness.client.status().mode, "private");
  assert.equal(harness.client.status().participantId, null);
  assert.equal([...harness.storage.values()].some((value) => value.includes("participantId")), false);
});

test("join queues once and removes only acknowledged events", async () => {
  const harness = createHarness();
  harness.client.join();
  await harness.client.recordStarted("orchard", "external");
  assert.equal(harness.requests.length, 1);
  assert.equal(harness.client.status().pending, 0);
});

test("network failure keeps a bounded outbox without breaking play", async () => {
  const harness = createHarness({ responseStatus: 503 });
  harness.client.join();
  for (let index = 0; index < 60; index += 1) {
    await harness.client.recordRetry("orchard");
  }
  assert.equal(harness.client.status().pending, 50);
});

test("delete removes server and local telemetry state only after success", async () => {
  const harness = createHarness();
  harness.client.join();
  const participantId = harness.client.status().participantId;
  assert.ok(participantId);
  await harness.client.deleteData();
  assert.equal(harness.requests.at(-1).url.endsWith("/v1/delete"), true);
  assert.equal(harness.client.status().participantId, null);
});
```

The test helper supplies deterministic UUIDs, dates, storage, and fetch so no
browser globals are needed.

- [ ] **Step 2: Confirm the client tests fail**

Run:

```bash
node --test test/playtest-client.test.js
```

Expected: FAIL with `ERR_MODULE_NOT_FOUND`.

- [ ] **Step 3: Implement the adapter with a narrow public API**

`createPlaytestClient` accepts all side effects:

```js
export function createPlaytestClient({
  config,
  endpoint,
  storage,
  fetchImpl,
  randomUUID,
  now,
}) {
  // Public methods:
  // status()
  // join()
  // choosePrivate()
  // recordStarted(routeId, launchContext)
  // recordFinished(routeId, summary)
  // recordRetry(routeId)
  // flush()
  // deleteData()
}
```

Use these exact scoped keys:

```js
const STATE_PREFIX = "wobble-stack-playtest:v1";
const CHOICE_PREFIX = "wobble-stack-playtest-choice:v1";
const MAX_PENDING_EVENTS = 50;
```

Required behavior:

- `config === null` or an empty/non-HTTPS production endpoint returns inactive
  mode and never reads or writes storage.
- `choosePrivate()` stores only the string `private` under the scoped choice
  key; it creates no UUID and makes every record method a no-op.
- `join()` creates one UUID, sequence `0`, and an empty outbox, then persists it.
- Every event increments sequence, uses `getLocalDay(now())`, appends to the
  bounded outbox, persists before network I/O, and calls `flush()` without
  throwing into gameplay.
- `flush()` sends `{ events: [...] }` to `${endpoint}/v1/events`, removes only
  the exact acknowledged event IDs after a 2xx response, and coalesces
  concurrent flushes.
- A failed request leaves the queue intact and returns `{ delivered: false }`.
- `deleteData()` posts `{ version: 1, cohort, participantId }`, and clears the
  scoped state and choice keys only after a successful response.
- Malformed stored JSON is removed only for the current scoped key and restarts
  at undecided mode.

- [ ] **Step 4: Run client, logic, and full tests**

```bash
node --test test/playtest-client.test.js test/playtest-logic.test.js
pnpm test
```

Expected: PASS with no network or browser dependency.

- [ ] **Step 5: Commit the client boundary**

```bash
git add src/playtest-client.js test/playtest-client.test.js
git commit -m "feat: add resilient local playtest client"
```

### Task 3: Consent UI and exact game-transition integration

**Files:**
- Modify: `index.html` start overlay
- Modify: `src/style.css` responsive overlays
- Modify: `src/game.js` imports, controls, run counters, result hooks, Telegram
  link, and debug surface
- Modify: `test/playtest-logic.test.js`

- [ ] **Step 1: Add a failing semantic-reason test**

Add a pure helper to `src/playtest-logic.js` and test it:

```js
test("only loss-result Retry is a retry event", () => {
  assert.equal(isRetryReason("retry"), true);
  for (const reason of ["start", "replay", "next", "debug", "route"]) {
    assert.equal(isRetryReason(reason), false);
  }
});
```

Expected first run: FAIL because `isRetryReason` is not exported.

- [ ] **Step 2: Implement the helper**

```js
export function isRetryReason(reason) {
  return reason === "retry";
}
```

- [ ] **Step 3: Add the consent overlay**

Place a sibling overlay after `#loading-overlay`:

```html
<div id="playtest-overlay" class="game-overlay playtest-overlay" hidden>
  <div class="playtest-card">
    <p class="eyebrow">OPTIONAL PLAYTEST</p>
    <h2>Help improve the wobble</h2>
    <p>
      This test sends anonymous round summaries. No name, Telegram account,
      messages, or precise controls are collected.
    </p>
    <button id="join-playtest-button" class="primary-button" type="button">
      Play and join the test
    </button>
    <button id="private-play-button" class="text-button" type="button">
      Play privately
    </button>
  </div>
</div>
```

Add a playtest-only status area immediately after the start footer so the
existing music/reset flex row keeps its layout:

```html
<div id="playtest-status" class="playtest-status" hidden>
  <span id="playtest-status-copy"></span>
  <button id="delete-playtest-button" class="text-button" type="button">
    Delete test data
  </button>
</div>
```

- [ ] **Step 4: Style both 320 × 700 and normal portrait sizes**

Use a centered modal card with a maximum width of `330px`, 44-pixel actions,
the existing clay palette, and `z-index: 14`. Under `max-height: 730px`, reduce
card padding and heading size, never the action height. Add the new buttons to
the existing focus-visible selector.

- [ ] **Step 5: Wire explicit run reasons and counters**

Initialize the client once:

```js
const playtestConfig = parsePlaytestConfig(window.location.search);
const playtest = createPlaytestClient({
  config: playtestConfig,
  endpoint: import.meta.env.VITE_PLAYTEST_ENDPOINT || "",
  storage: window.localStorage,
  fetchImpl: window.fetch.bind(window),
  randomUUID: crypto.randomUUID.bind(crypto),
  now: () => new Date(),
});
```

Replace shared button bindings with explicit reasons:

```js
startButton.addEventListener("click", () => startRun("start"));
retryButton.addEventListener("click", () => {
  playtest.recordRetry(currentRoute.id);
  startRun("retry");
});
replayButton.addEventListener("click", () => startRun("replay"));
```

`startNextRoute()` calls `startRun("next")`. The debug start calls
`startRun("debug")` and therefore cannot create Retry.

At each run reset:

```js
let runJumpCount = 0;

function startRun(reason = "start") {
  // Existing start behavior remains in its current order.
  runJumpCount = 0;
  playtest.recordStarted(currentRoute.id, launchContext);
}
```

Increment `runJumpCount` only after `triggerJump()` passes every guard. At
`showResults()` and `completeRoute()`, emit one finished event using:

```js
function getPlaytestRoundSummary(outcome) {
  const outcomes = [...obstacleOutcomes.values()];
  return {
    outcome,
    durationSeconds: runSeconds,
    progressPercent: getRouteCompletion(
      journeyProgress,
      currentRoute.finishDistance,
    ) * 100,
    badges: collectedBadges.size,
    jumps: runJumpCount,
    obstaclesHit: outcomes.filter((value) => value === "hit").length,
    obstaclesCleared: outcomes.filter((value) => value === "cleared").length,
  };
}
```

State guards already make `showResults()` and `completeRoute()` one-shot; add
debug assertions for the emitted counters.

- [ ] **Step 6: Preserve the full Telegram query**

Build the handoff URL from the current URL, not the canonical URL:

```js
const gameUrl = new URL(window.location.href);
gameUrl.hash = "";
```

Pass `gameUrl.href` to `openLink` and clipboard. Determine `launchContext`
from the Telegram bridge, explicit `from=telegram`, or Telegram user agent; do
not confuse the distribution `source=telegram` label with current runtime.

- [ ] **Step 7: Wire consent, private play, and deletion**

- In inactive mode, never show the playtest overlay or status.
- In undecided active mode, show consent only after art loads.
- Join and Private both resolve the choice and call `startRun("start")` from the
  same user gesture.
- On later joined visits, show the start menu normally, flush pending events,
  and show `Anonymous playtest active`.
- On later private visits, show ordinary play with no telemetry status.
- The first delete click changes text to `Tap again to delete`; the second calls
  `deleteData()`. Success returns to undecided mode without deleting journey or
  music storage.
- Clipboard is not required anywhere in the new flow.

- [ ] **Step 8: Run tests and production build**

```bash
pnpm test
VITE_PLAYTEST_ENDPOINT=https://collector.invalid pnpm build
git diff --check
```

Expected: all tests PASS, Vite builds, and no whitespace errors appear.

- [ ] **Step 9: Commit the browser integration**

```bash
git add index.html src/style.css src/game.js src/playtest-logic.js test/playtest-logic.test.js
git commit -m "feat: instrument opt-in browser playtests"
```

### Task 4: Cloudflare validation and HTTP boundary

**Files:**
- Create: `worker/src/validation.js`
- Create: `worker/src/index.js`
- Create: `worker/test/validation.test.js`
- Create: `worker/test/worker.test.js`
- Create: `worker/wrangler.jsonc`
- Modify: `package.json`
- Modify: `pnpm-lock.yaml`

- [ ] **Step 1: Apply the dependency gate**

Use `lazy-senior` before adding Wrangler. The expected decision is to accept one
official deployment dependency (`wrangler`) and reject frameworks, ORMs,
analytics SDKs, and dashboard libraries because the Worker uses platform APIs
directly.

- [ ] **Step 2: Verify and add current Wrangler**

```bash
pnpm view wrangler version
pnpm add -D wrangler
pnpm wrangler --version
```

Expected: Wrangler is at least `4.36.0`, required by Rate Limiting bindings.

- [ ] **Step 3: Write failing validator tests**

Cover:

```js
test("validator accepts the three exact semantic event shapes", () => {
  assert.equal(validateBatch({ events: [started, finished, retry] }).length, 3);
});

test("validator rejects unknown fields and forbidden identifiers", () => {
  assert.throws(
    () => validateBatch({ events: [{ ...started, telegramId: "123" }] }),
    /field/i,
  );
});

test("validator rejects oversized, unknown, and out-of-range values", () => {
  assert.throws(() => validateBatch({ events: [] }), /events/i);
  assert.throws(
    () => validateBatch({ events: [{ ...finished, durationSeconds: 1801 }] }),
    /durationSeconds/,
  );
});
```

- [ ] **Step 4: Implement strict validation**

`validateBatch(input, env)` must:

- require a plain `{ events }` object and 1–20 events;
- reject unknown keys, not merely ignore them;
- enforce version `1`, active build, active cohort/source allow-lists, UUIDs,
  local day, sequence `1..1000000`, and route IDs;
- require every event in a batch to use the same participant, cohort, build,
  and source so the request has one unambiguous rate-limit key;
- enforce event-specific nullable fields;
- return a new normalized object without the original input reference.

Parse allow-lists from:

```js
const builds = new Set(env.ACTIVE_BUILDS.split(","));
const cohorts = new Set(env.ACTIVE_COHORTS.split(","));
const sources = new Set(env.ACTIVE_SOURCES.split(","));
```

- [ ] **Step 5: Write failing Worker request tests**

The binding fake records prepared SQL and returns deterministic success. Test:

- `GET /health` returns 200 and no cohort data;
- disallowed origins return 403;
- OPTIONS returns exact CORS headers only for the game origin;
- invalid or over-4096-byte JSON returns 400/413 without echoing input;
- a rejected rate limit returns 429 before D1 writes;
- accepted events use prepared/bound statements and return acknowledged IDs;
- `/v1/delete` deletes events and anonymizes a cohort slot;
- unknown routes return 404;
- `scheduled()` runs the 30-day deletion statement.

- [ ] **Step 6: Implement the Worker**

Use one module export:

```js
export default {
  fetch(request, env) {
    return handleRequest(request, env);
  },
  scheduled(controller, env, context) {
    context.waitUntil(
      env.DB.prepare(
        "DELETE FROM events WHERE received_at < unixepoch() - 2592000",
      ).run(),
    );
  },
};
```

`POST /v1/events`:

1. Verify `Origin === env.GAME_ORIGIN`.
2. Reject bodies over 4096 bytes before JSON parsing.
3. Validate the batch.
4. Rate-limit on `${cohort}:${participantId}` using
   `env.EVENTS_RATE_LIMITER.limit({ key })`.
5. Build parameterized `INSERT OR IGNORE` statements.
6. For each `round_started`, add a slot statement that inserts only while the
   cohort has fewer than 20 rows.
7. Execute one `env.DB.batch(statements)` call.
8. Return `{ accepted: [eventId...] }` with the exact CORS origin.

Do not call `console.log`, persist headers, or return submitted fields in error
messages.

Use this parameterized slot statement; D1 batch execution keeps it in the same
transactional request as the event insert:

```sql
INSERT INTO cohort_members (cohort, ordinal, participant_id)
SELECT ?, COUNT(*) + 1, ?
FROM cohort_members
WHERE cohort = ?
HAVING COUNT(*) < 20
ON CONFLICT DO NOTHING
```

Apply the same participant-key rate limiter to `/v1/delete`.

- [ ] **Step 7: Add Worker configuration**

Create `worker/wrangler.jsonc`:

```jsonc
{
  "$schema": "../node_modules/wrangler/config-schema.json",
  "name": "wobble-stack-playtest",
  "main": "src/index.js",
  "compatibility_date": "2026-07-30",
  "workers_dev": true,
  "observability": {
    "enabled": false
  },
  "vars": {
    "GAME_ORIGIN": "https://kiku-jw.github.io",
    "ACTIVE_BUILDS": "TG1-01",
    "ACTIVE_COHORTS": "TG1,TG1-PILOT",
    "ACTIVE_SOURCES": "telegram,website,pilot"
  },
  "ratelimits": [
    {
      "name": "EVENTS_RATE_LIMITER",
      "namespace_id": "764211",
      "simple": {
        "limit": 20,
        "period": 60
      }
    }
  ],
  "triggers": {
    "crons": ["17 3 * * *"]
  }
}
```

Add scripts:

```json
{
  "scripts": {
    "test": "node --test test/*.test.js worker/test/*.test.js",
    "worker:check": "wrangler deploy --dry-run --config worker/wrangler.jsonc",
    "worker:dev": "wrangler dev --config worker/wrangler.jsonc --var GAME_ORIGIN:http://127.0.0.1:5173",
    "worker:deploy": "wrangler deploy --config worker/wrangler.jsonc",
    "check": "pnpm test && pnpm build && pnpm worker:check"
  }
}
```

- [ ] **Step 8: Run Worker and full checks**

```bash
node --test worker/test/validation.test.js worker/test/worker.test.js
pnpm check
git diff --check
```

Expected: all tests PASS and Wrangler dry-run produces a Worker bundle without
deploying it.

- [ ] **Step 9: Commit the collector boundary**

```bash
git add package.json pnpm-lock.yaml worker
git commit -m "feat: add private playtest collector"
```

### Task 5: D1 schema, locked cohort, and private report

**Files:**
- Create: `worker/migrations/0001_playtest.sql`
- Create: `worker/queries/cohort-summary.sql`
- Modify: `worker/test/worker.test.js`

- [ ] **Step 1: Write the D1 schema**

```sql
CREATE TABLE events (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  event_id TEXT NOT NULL UNIQUE,
  participant_id TEXT NOT NULL,
  cohort TEXT NOT NULL,
  build TEXT NOT NULL,
  source TEXT NOT NULL,
  event_type TEXT NOT NULL,
  local_day TEXT NOT NULL,
  client_sequence INTEGER NOT NULL,
  route_id TEXT NOT NULL,
  launch_context TEXT,
  outcome TEXT,
  duration_seconds INTEGER,
  progress_percent INTEGER,
  badges INTEGER,
  jumps INTEGER,
  obstacles_hit INTEGER,
  obstacles_cleared INTEGER,
  received_at INTEGER NOT NULL DEFAULT (unixepoch())
);

CREATE INDEX events_cohort_participant
  ON events (cohort, participant_id, id);
CREATE INDEX events_retention
  ON events (received_at);

CREATE TABLE cohort_members (
  cohort TEXT NOT NULL,
  ordinal INTEGER NOT NULL CHECK (ordinal BETWEEN 1 AND 20),
  participant_id TEXT,
  first_started_at INTEGER NOT NULL DEFAULT (unixepoch()),
  deleted_at INTEGER,
  PRIMARY KEY (cohort, ordinal),
  UNIQUE (cohort, participant_id)
);
```

The slot assignment statement uses `COUNT(*) + 1` only when fewer than 20 slot
rows exist. Deletion sets `participant_id = NULL` instead of removing the slot,
so a withdrawn participant is never silently replaced.

- [ ] **Step 2: Test deduplication, slot locking, and withdrawal**

Extend the binding fake or run a local D1 integration to prove:

- duplicate `event_id` creates one row;
- the first 20 distinct starters get ordinals 1–20;
- starter 21 creates events but no primary slot;
- replaying a starter does not create another slot;
- deletion removes that participant’s events and anonymizes their occupied
  slot;
- participant 21 still does not move into the primary cohort.

- [ ] **Step 3: Add the aggregate query**

`worker/queries/cohort-summary.sql` returns one row with:

```sql
WITH primary_rounds AS (
  SELECT
    m.ordinal,
    e.participant_id,
    COUNT(*) FILTER (WHERE e.event_type = 'round_started') AS rounds_started,
    COUNT(DISTINCT CASE
      WHEN e.event_type = 'round_started' THEN e.local_day
    END) AS play_days,
    COUNT(*) FILTER (WHERE e.event_type = 'retry_clicked') AS retries,
    SUM(CASE WHEN e.event_type = 'round_finished' THEN e.jumps ELSE 0 END) AS jumps,
    SUM(CASE WHEN e.event_type = 'round_finished' THEN e.obstacles_hit ELSE 0 END) AS hits,
    SUM(CASE WHEN e.event_type = 'round_finished' THEN e.obstacles_cleared ELSE 0 END) AS clears
  FROM cohort_members m
  LEFT JOIN events e
    ON e.cohort = m.cohort AND e.participant_id = m.participant_id
  WHERE m.cohort = 'TG1'
  GROUP BY m.ordinal, e.participant_id
)
SELECT
  COUNT(*) AS primary_slots,
  COUNT(participant_id) AS retained_participants,
  COUNT(*) FILTER (WHERE rounds_started >= 5) AS repeat_5,
  COUNT(*) FILTER (WHERE play_days >= 2) AS returned_other_day,
  SUM(retries) AS retries,
  SUM(jumps) AS jumps,
  SUM(hits) AS obstacle_hits,
  SUM(clears) AS obstacle_clears
FROM primary_rounds;
```

Append one grouped diagnostic result set using only primary-cohort aggregates:

```sql
SELECT
  e.source,
  e.route_id,
  e.outcome,
  COUNT(*) AS finished_rounds,
  ROUND(AVG(e.duration_seconds)) AS average_duration_seconds,
  ROUND(AVG(e.progress_percent)) AS average_progress_percent,
  SUM(e.badges) AS badges,
  SUM(e.jumps) AS jumps,
  SUM(e.obstacles_hit) AS obstacle_hits,
  SUM(e.obstacles_cleared) AS obstacle_clears
FROM events e
JOIN cohort_members m
  ON m.cohort = e.cohort AND m.participant_id = e.participant_id
WHERE e.cohort = 'TG1' AND e.event_type = 'round_finished'
GROUP BY e.source, e.route_id, e.outcome
ORDER BY e.source, e.route_id, e.outcome;
```

- [ ] **Step 4: Provision the authorized EU D1 binding once**

Verify authentication, then create exactly one database and let Wrangler write
its opaque ID into the existing config:

```bash
pnpm wrangler whoami
pnpm wrangler d1 create wobble-stack-playtest \
  --binding DB \
  --jurisdiction eu \
  --update-config \
  --config worker/wrangler.jsonc
```

Read back `pnpm wrangler d1 list` before retrying any unclear response. Never
create a second database with the same purpose.

- [ ] **Step 5: Apply and inspect the migration locally**

Run:

```bash
pnpm wrangler d1 migrations apply wobble-stack-playtest \
  --local \
  --config worker/wrangler.jsonc
pnpm wrangler d1 execute wobble-stack-playtest \
  --local \
  --command="SELECT name FROM sqlite_master WHERE type='table' ORDER BY name" \
  --config worker/wrangler.jsonc
```

Expected tables include `cohort_members`, `events`, and Wrangler’s migration
table.

- [ ] **Step 6: Commit schema, binding, and report**

```bash
git add worker/wrangler.jsonc worker/migrations worker/queries worker/test/worker.test.js worker/src/index.js
git commit -m "feat: lock and report the playtest cohort"
```

### Task 6: Local browser and failure-path verification

**Files:**
- Modify: `docs/TEST_PLAN.md`
- Modify: `docs/IMPLEMENTATION_NOTES.md`
- Optional evidence only: `.agent/tasks/issue-4-public-playtest/`

- [ ] **Step 1: Start the local collector and game**

Terminal A:

```bash
pnpm worker:dev
```

Terminal B:

```bash
VITE_PLAYTEST_ENDPOINT=http://127.0.0.1:8787 pnpm dev
```

- [ ] **Step 2: Verify ordinary and private modes**

At 320 × 700, 390 × 844, and desktop:

- `/` shows no consent UI, creates no playtest keys, and makes no collector
  requests;
- `?playtest=TG1-PILOT&source=pilot` shows the consent overlay;
- `Play privately` starts gameplay, creates no UUID, and sends no events;
- reload preserves the private choice without affecting journey/music storage.

- [ ] **Step 3: Verify joined event semantics**

In a clean context:

- Join sends exactly one `round_started`.
- A successful tap increments jumps; a drag does not.
- Grounded obstacle contact increments hit; a valid jump increments clear.
- Collapse sends exactly one loss `round_finished`.
- Retry sends one `retry_clicked` and one new `round_started`.
- Completed-route Replay and Next road send new starts but no Retry.
- Reload does not invent a round, Retry, or different-day return.

- [ ] **Step 4: Verify network isolation and recovery**

Stop the Worker during a run. Confirm gameplay and results remain responsive,
pending count grows, and no uncaught error appears. Restart the Worker and
reload; confirm queued event IDs arrive once and the queue empties.

- [ ] **Step 5: Verify deletion**

Use `Delete test data`, confirm twice, then query local D1:

```bash
pnpm wrangler d1 execute wobble-stack-playtest \
  --local \
  --command="SELECT COUNT(*) AS events FROM events" \
  --config worker/wrangler.jsonc
```

Expected: the participant’s events are gone, its primary slot remains
anonymized if it had one, and ordinary journey/music preferences remain.

- [ ] **Step 6: Verify Telegram handoff**

Open:

```text
http://127.0.0.1:5173/?from=telegram&playtest=TG1-PILOT&source=pilot
```

Confirm Copy/Open externally retains all three query fields. Dismiss and play
paths remain usable at 320 × 700 with no horizontal/document overflow.

- [ ] **Step 7: Record the durable test procedure**

Add the above cases to `docs/TEST_PLAN.md`. In
`docs/IMPLEMENTATION_NOTES.md`, document event boundaries, first-20 slot
locking, 30-day deletion, and why no input stream or third-party SDK exists.

- [ ] **Step 8: Run final local checks and commit**

```bash
pnpm check
git diff --check
git add docs/TEST_PLAN.md docs/IMPLEMENTATION_NOTES.md
git commit -m "docs: define public playtest verification"
```

### Task 7: Provision and deploy the authorized Cloudflare collector

**Files:**
- Modify automatically: `worker/wrangler.jsonc` with the real D1 binding ID
- Modify: `.github/workflows/deploy-pages.yml`
- Modify: `docs/STATUS.md`

- [ ] **Step 1: Re-verify Cloudflare authentication without exposing credentials**

```bash
pnpm wrangler whoami
```

Expected: an authenticated account and permissions for Workers, D1, and account
settings. If authentication is absent, use the official Wrangler browser login;
never copy tokens into chat or Git.

- [ ] **Step 2: Verify the single D1 target before remote mutation**

```bash
pnpm wrangler d1 list
pnpm wrangler d1 info wobble-stack-playtest
```

Expected: exactly one `wobble-stack-playtest` database and the same opaque ID
already committed in `worker/wrangler.jsonc`.

- [ ] **Step 3: Apply the migration remotely and inspect tables**

```bash
pnpm wrangler d1 migrations apply wobble-stack-playtest \
  --remote \
  --config worker/wrangler.jsonc
pnpm wrangler d1 execute wobble-stack-playtest \
  --remote \
  --command="SELECT name FROM sqlite_master WHERE type='table' ORDER BY name" \
  --config worker/wrangler.jsonc
```

Expected: `cohort_members` and `events` exist with zero cohort rows.

- [ ] **Step 4: Dry-run, deploy, and inspect the Worker**

```bash
pnpm worker:check
pnpm worker:deploy
pnpm wrangler deployments list --config worker/wrangler.jsonc
```

Record the exact HTTPS `workers.dev` URL returned by deploy. Check `/health`
before allowing the game to reference it.

- [ ] **Step 5: Configure the Pages build with the public URL**

Update the Pages workflow build step:

```yaml
      - name: Build site
        env:
          VITE_PLAYTEST_ENDPOINT: ${{ vars.PLAYTEST_ENDPOINT }}
        run: pnpm build
```

Set repository variable `PLAYTEST_ENDPOINT` to the exact HTTPS URL printed by
the successful Worker deployment. It is a public endpoint, not a secret.
Immediately read the variable name back with `gh variable list`; do not print
credentials.

- [ ] **Step 6: Run an excluded live pilot**

Use:

```text
https://kiku-jw.github.io/wobble-stack/?from=telegram&playtest=TG1-PILOT&source=pilot
```

After Pages deployment, prove health, CORS, Join, Start, Finish, Retry,
deduplication, delete, cron cleanup route locally, and the private report query.
Delete all `TG1-PILOT` rows before opening enrollment.

```bash
pnpm wrangler d1 execute wobble-stack-playtest \
  --remote \
  --command="DELETE FROM events WHERE cohort = 'TG1-PILOT'; DELETE FROM cohort_members WHERE cohort = 'TG1-PILOT';" \
  --config worker/wrangler.jsonc
```

- [ ] **Step 7: Commit deployment configuration and evidence**

```bash
git add worker/wrangler.jsonc .github/workflows/deploy-pages.yml docs/STATUS.md
git commit -m "ops: configure playtest collection"
```

### Task 8: Publish the frozen game and open the measured cohort

**Files:**
- Modify: `docs/STATUS.md`
- Modify: `docs/BUILD_DIARY.md`
- Modify: GitHub Issue `kiku-jw/wobble-stack#4`

- [ ] **Step 1: Run the completion matrix**

```bash
pnpm check
git diff --check
git status --short --branch
```

Require:

- all browser and Worker tests PASS;
- production game and Worker dry-run builds PASS;
- ordinary/private/joined paths PASS;
- mobile and Telegram full-query handoff PASS;
- offline outbox and deletion PASS;
- no secret, token, private ID, raw test event, or unexpected third-party host
  appears in the diff.

- [ ] **Step 2: Obtain a fresh independent review**

Review the complete diff against every section of the approved design. Fix all
material findings, rerun the relevant tests, and record only evidence that was
actually observed.

- [ ] **Step 3: Publish under repository hygiene**

Read `/Users/nick/.codex/rules/git-publish-hygiene.md`, verify `main` and remote
state, push the coherent commits, and wait for the existing GitHub Pages
workflow. Do not create another deployment mechanism for the static site.

- [ ] **Step 4: Perform fresh production smoke**

Verify the deployed asset, console, viewport matrix, ordinary mode, private
mode, joined pilot mode, Worker `/health`, D1 inserts, deletion, and aggregate
query. Remove pilot rows and verify `TG1` still has zero primary slots before
sharing the enrollment link.

- [ ] **Step 5: Publish the one measured link**

The owner-facing link is:

```text
https://kiku-jw.github.io/wobble-stack/?from=telegram&playtest=TG1&source=telegram
```

Nick may replace the old channel link or make a new post. If 20 starts are not
reached, use the same frozen build and cohort with an allow-listed alternate
source; do not change gameplay or event semantics mid-cohort.

- [ ] **Step 6: Synchronize Issue #4**

Update Issue #4 with:

- final game and Worker commits;
- Pages and Worker deployment evidence;
- exact enrollment build `TG1-01`;
- primary cohort initially `0/20`;
- the prior three-friend qualitative result and exposure limitation;
- private aggregate-query command;
- 12/20 Repeat and 6/20 Return gates;
- next actor: Nick to replace/repost the link and wait for the observation
  window.

- [ ] **Step 7: Commit final durable evidence**

```bash
git add docs/STATUS.md docs/BUILD_DIARY.md
git commit -m "docs: open the anonymous playtest cohort"
```

After the cohort closes, create one immutable experiment receipt, delete raw
events, supersede the temporary design/plan, and decide the next product change
from the predeclared gates.
