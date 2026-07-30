export const EVENT_INSERT = `
  INSERT OR IGNORE INTO events (
    event_id,
    participant_id,
    cohort,
    build,
    source,
    event_type,
    local_day,
    client_sequence,
    route_id,
    launch_context,
    outcome,
    duration_seconds,
    progress_percent,
    badges,
    jumps,
    obstacles_hit,
    obstacles_cleared
  ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
`;

export const COHORT_SLOT_INSERT = `
  INSERT INTO cohort_members (cohort, ordinal, participant_id)
  SELECT ?, COUNT(*) + 1, ?
  FROM cohort_members
  WHERE cohort = ?
  HAVING COUNT(*) < 20
  ON CONFLICT DO NOTHING
`;

export const DELETE_EVENTS = `
  DELETE FROM events
  WHERE cohort = ? AND participant_id = ?
`;

export const WITHDRAW_COHORT_SLOT = `
  UPDATE cohort_members
  SET participant_id = NULL, deleted_at = unixepoch()
  WHERE cohort = ? AND participant_id = ?
`;
