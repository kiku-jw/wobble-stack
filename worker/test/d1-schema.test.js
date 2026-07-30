import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { DatabaseSync } from "node:sqlite";
import test from "node:test";

import {
  COHORT_SLOT_INSERT,
  DELETE_EVENTS,
  EVENT_INSERT,
  WITHDRAW_COHORT_SLOT,
} from "../src/statements.js";

const migrationUrl = new URL(
  "../migrations/0001_playtest.sql",
  import.meta.url,
);
const summaryUrl = new URL(
  "../queries/cohort-summary.sql",
  import.meta.url,
);
const migration = readFileSync(migrationUrl, "utf8");
const summary = readFileSync(summaryUrl, "utf8");

function participant(index) {
  return `00000000-0000-4000-8000-${String(index).padStart(12, "0")}`;
}

function insertEvent(database, {
  id = "99999999-9999-4999-8999-999999999999",
  participantId = participant(1),
} = {}) {
  database.prepare(EVENT_INSERT).run(
    id,
    participantId,
    "TG1",
    "TG1-01",
    "telegram",
    "round_started",
    "2026-07-30",
    1,
    "orchard",
    "telegram",
    null,
    null,
    null,
    null,
    null,
    null,
    null,
  );
}

test("schema deduplicates events by public event ID", () => {
  const database = new DatabaseSync(":memory:");
  database.exec(migration);

  insertEvent(database);
  insertEvent(database);

  assert.equal(
    database.prepare("SELECT COUNT(*) AS count FROM events").get().count,
    1,
  );
  database.close();
});

test("first twenty starters keep fixed slots after withdrawal", () => {
  const database = new DatabaseSync(":memory:");
  database.exec(migration);
  const assignSlot = database.prepare(COHORT_SLOT_INSERT);

  for (let index = 1; index <= 21; index += 1) {
    assignSlot.run("TG1", participant(index), "TG1");
  }
  assignSlot.run("TG1", participant(1), "TG1");

  const countSlots = database.prepare(
    "SELECT COUNT(*) AS count FROM cohort_members WHERE cohort = 'TG1'",
  );
  assert.equal(countSlots.get().count, 20);
  assert.equal(
    database.prepare(
      "SELECT COUNT(*) AS count FROM cohort_members WHERE participant_id = ?",
    ).get(participant(21)).count,
    0,
  );

  insertEvent(database, { participantId: participant(5) });
  database.prepare(DELETE_EVENTS).run("TG1", participant(5));
  database.prepare(WITHDRAW_COHORT_SLOT).run("TG1", participant(5));
  assignSlot.run("TG1", participant(21), "TG1");

  const withdrawn = database.prepare(
    "SELECT ordinal, participant_id, deleted_at FROM cohort_members WHERE cohort = 'TG1' AND ordinal = 5",
  ).get();
  assert.equal(withdrawn.ordinal, 5);
  assert.equal(withdrawn.participant_id, null);
  assert.equal(typeof withdrawn.deleted_at, "number");
  assert.equal(countSlots.get().count, 20);
  assert.equal(
    database.prepare(
      "SELECT COUNT(*) AS count FROM cohort_members WHERE participant_id = ?",
    ).get(participant(21)).count,
    0,
  );
  assert.equal(
    database.prepare("SELECT COUNT(*) AS count FROM events").get().count,
    0,
  );
  database.close();
});

test("private aggregate report compiles against the schema", () => {
  const database = new DatabaseSync(":memory:");
  database.exec(migration);
  database.exec(summary);
  database.close();
});
