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
