WITH primary_rounds AS (
  SELECT
    m.ordinal,
    m.participant_id,
    COUNT(*) FILTER (
      WHERE e.event_type = 'round_started'
    ) AS rounds_started,
    COUNT(DISTINCT CASE
      WHEN e.event_type = 'round_started' THEN e.local_day
    END) AS play_days,
    COUNT(*) FILTER (
      WHERE e.event_type = 'retry_clicked'
    ) AS retries,
    SUM(
      CASE WHEN e.event_type = 'round_finished' THEN e.jumps ELSE 0 END
    ) AS jumps,
    SUM(
      CASE WHEN e.event_type = 'round_finished' THEN e.obstacles_hit ELSE 0 END
    ) AS hits,
    SUM(
      CASE WHEN e.event_type = 'round_finished'
        THEN e.obstacles_cleared
        ELSE 0
      END
    ) AS clears
  FROM cohort_members m
  LEFT JOIN events e
    ON e.cohort = m.cohort AND e.participant_id = m.participant_id
  WHERE m.cohort = 'TG1'
  GROUP BY m.ordinal, m.participant_id
)
SELECT
  COUNT(*) AS primary_slots,
  COUNT(participant_id) AS retained_participants,
  COUNT(*) FILTER (WHERE rounds_started >= 5) AS repeat_5,
  COUNT(*) FILTER (WHERE play_days >= 2) AS returned_other_day,
  COALESCE(SUM(retries), 0) AS retries,
  COALESCE(SUM(jumps), 0) AS jumps,
  COALESCE(SUM(hits), 0) AS obstacle_hits,
  COALESCE(SUM(clears), 0) AS obstacle_clears
FROM primary_rounds;

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
