import {
  validateBatch,
  validateDeletion,
} from "./validation.js";
import {
  COHORT_SLOT_INSERT,
  DELETE_EVENTS,
  EVENT_INSERT,
  WITHDRAW_COHORT_SLOT,
} from "./statements.js";

const MAX_BODY_BYTES = 4096;

function corsHeaders(origin) {
  return {
    "Access-Control-Allow-Origin": origin,
    "Access-Control-Allow-Methods": "POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
    "Access-Control-Max-Age": "86400",
    Vary: "Origin",
  };
}

function json(payload, status = 200, origin = null) {
  const headers = {
    "Content-Type": "application/json; charset=utf-8",
    ...(origin ? corsHeaders(origin) : {}),
  };
  return new Response(JSON.stringify(payload), { status, headers });
}

function empty(status, origin = null) {
  return new Response(null, {
    status,
    headers: origin ? corsHeaders(origin) : {},
  });
}

function isAllowedOrigin(request, env) {
  return request.headers.get("Origin") === env.GAME_ORIGIN;
}

async function readJsonBody(request) {
  const contentLength = Number(request.headers.get("Content-Length") || 0);
  if (Number.isFinite(contentLength) && contentLength > MAX_BODY_BYTES) {
    return { error: "too_large" };
  }

  let text;
  try {
    text = await request.text();
  } catch {
    return { error: "invalid" };
  }
  if (new TextEncoder().encode(text).byteLength > MAX_BODY_BYTES) {
    return { error: "too_large" };
  }

  try {
    return { value: JSON.parse(text) };
  } catch {
    return { error: "invalid" };
  }
}

function eventStatement(db, event) {
  return db.prepare(EVENT_INSERT).bind(
    event.eventId,
    event.participantId,
    event.cohort,
    event.build,
    event.source,
    event.type,
    event.localDay,
    event.clientSequence,
    event.routeId,
    event.launchContext ?? null,
    event.outcome ?? null,
    event.durationSeconds ?? null,
    event.progressPercent ?? null,
    event.badges ?? null,
    event.jumps ?? null,
    event.obstaclesHit ?? null,
    event.obstaclesCleared ?? null,
  );
}

async function rateLimit(env, cohort, participantId) {
  const result = await env.EVENTS_RATE_LIMITER.limit({
    key: `${cohort}:${participantId}`,
  });
  return result.success;
}

async function handleEvents(request, env) {
  const origin = env.GAME_ORIGIN;
  const body = await readJsonBody(request);
  if (body.error === "too_large") {
    return json({ error: "request_too_large" }, 413, origin);
  }
  if (body.error) {
    return json({ error: "invalid_request" }, 400, origin);
  }

  let events;
  try {
    events = validateBatch(body.value, env);
  } catch {
    return json({ error: "invalid_request" }, 400, origin);
  }

  const first = events[0];
  if (!await rateLimit(env, first.cohort, first.participantId)) {
    return json({ error: "rate_limited" }, 429, origin);
  }

  const statements = [];
  for (const event of events) {
    statements.push(eventStatement(env.DB, event));
    if (event.type === "round_started") {
      statements.push(
        env.DB.prepare(COHORT_SLOT_INSERT).bind(
          event.cohort,
          event.participantId,
          event.cohort,
        ),
      );
    }
  }
  await env.DB.batch(statements);

  return json({ accepted: events.map((event) => event.eventId) }, 200, origin);
}

async function handleDelete(request, env) {
  const origin = env.GAME_ORIGIN;
  const body = await readJsonBody(request);
  if (body.error === "too_large") {
    return json({ error: "request_too_large" }, 413, origin);
  }
  if (body.error) {
    return json({ error: "invalid_request" }, 400, origin);
  }

  let deletion;
  try {
    deletion = validateDeletion(body.value, env);
  } catch {
    return json({ error: "invalid_request" }, 400, origin);
  }

  if (!await rateLimit(env, deletion.cohort, deletion.participantId)) {
    return json({ error: "rate_limited" }, 429, origin);
  }

  await env.DB.batch([
    env.DB.prepare(DELETE_EVENTS).bind(
      deletion.cohort,
      deletion.participantId,
    ),
    env.DB.prepare(WITHDRAW_COHORT_SLOT).bind(
      deletion.cohort,
      deletion.participantId,
    ),
  ]);

  return json({ deleted: true }, 200, origin);
}

export async function handleRequest(request, env) {
  const { pathname } = new URL(request.url);
  const isApiRoute = pathname === "/v1/events" || pathname === "/v1/delete";

  if (request.method === "GET" && pathname === "/health") {
    return json({ ok: true });
  }

  if (!isApiRoute) {
    return json({ error: "not_found" }, 404);
  }

  if (!isAllowedOrigin(request, env)) {
    return json({ error: "forbidden" }, 403);
  }

  if (request.method === "OPTIONS") {
    return empty(204, env.GAME_ORIGIN);
  }
  if (request.method !== "POST") {
    return json({ error: "not_found" }, 404, env.GAME_ORIGIN);
  }

  return pathname === "/v1/events"
    ? handleEvents(request, env)
    : handleDelete(request, env);
}

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
