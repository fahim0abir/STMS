# PROMPTS.md - AI Task Description Enhancement

## 1. Purpose

`POST /api/tasks/enhance-description` converts a rough, informal task note into a **structured, actionable task
definition** that a team can execute without follow-up questions. The model is `gemini-3.1-flash-lite` via Google AI
Studio, called through the official `Google.GenAI` SDK; the endpoint degrades gracefully to a deterministic local fallback.

## 2. Prompt Design

### System prompt (verbatim)

```
You are a senior delivery lead who turns rough engineering notes into precise, actionable work items.
Return a single JSON object and nothing else. No markdown fences, no commentary.

Schema:
{
  "enhancedDescription": "string",
  "suggestedTitle": "string",
  "acceptanceCriteria": ["string"],
  "subtasks": ["string"],
  "suggestedPriority": "Low" | "Medium" | "High" | "Critical",
  "estimatedHours": number | null
}

Rules:
- Fix grammar and spelling; keep the author's intent and technical vocabulary.
- enhancedDescription: professional tone, at most 1200 characters, structured as short sentences or hyphen bullets.
- suggestedTitle: imperative mood, at most 90 characters, no trailing period.
- acceptanceCriteria: 3 to 6 verifiable statements (testable, observable outcomes).
- subtasks: 2 to 6 concrete implementation steps, each starting with a verb.
- suggestedPriority: infer from impact, urgency words and scope; default to Medium when unclear.
- estimatedHours: realistic effort in hours (0.5 to 200) or null when the scope is unclear.
- Never invent external systems, credentials, or requirements that are not implied by the input.
```

### User prompt (template)

```
Rewrite the following task note into the required JSON schema.

Working title: {title}                  # optional
Project / domain context: {context}      # optional

Task note:
{description}
```

### Request parameters

| Parameter | Value | Rationale |
| --- | --- | --- |
| `temperature` | `0.4` | Stable, near-deterministic rewrites |
| `max_tokens` | `900` | Fits description + criteria + subtasks |
| `response_format` | `{ "type": "json_object" }` | Enforces parseable JSON (dropped automatically on the retry if the provider rejects it) |

## 3. Why structured output

The assignment asks for grammar, clarity, professionalism, expansion and actionable wording. Returning only a string
satisfies the wording but not the "actionable" intent, so the contract adds suggested title, acceptance criteria,
subtasks, priority and effort estimate. The Angular task modal consumes each field: it previews the suggestion,
applies the description, fills an empty title and sets the priority.

## 4. Example - input

```json
{
  "title": "login issue",
  "context": "SmartTask auth module",
  "description": "users say login breaks if their password has spaces or special chars. also the error message is confusing and stays for 10 mins. need fix asap"
}
```

## 5. Example - output

```json
{
  "enhancedDescription": "- Fix login failures for passwords containing spaces or special characters.\n- Replace the generic error message with a specific, actionable reason.\n- Clear the stale error state instead of persisting it for ten minutes.",
  "suggestedTitle": "Fix login failures for passwords with spaces or special characters",
  "acceptanceCriteria": [
    "Users with spaces or special characters in their password can authenticate successfully.",
    "Invalid credentials return a specific error message within one request.",
    "The error message is cleared after a successful login attempt."
  ],
  "subtasks": [
    "Reproduce the failure with a password containing a space.",
    "Harden credential parsing and trimming on the server.",
    "Update the login error mapping and clear the client-side error state."
  ],
  "suggestedPriority": "Critical",
  "estimatedHours": 4
}
```

## 6. Validation & Safety

**Input validation (FluentValidation, server-side; mirrored in Angular):**

- `description`: required, 20-4,000 characters (raised from the original 5-1,000 so multi-sentence notes are accepted).
- `title`: optional, max 200 characters.
- `context`: optional, max 500 characters.
- Rate limit: `enhance` fixed window, 10 requests/minute per client.
- Endpoint requires authentication (`[Authorize]`).

**Output hardening:**

- JSON is extracted with brace scanning; markdown fences and `**` markers are stripped.
- Every field is length-clamped (description 1,200, titles 90, list items 240) and lists are capped at 8 entries.
- `suggestedPriority` is parsed into the enum and defaults to `Medium`; `estimatedHours` is clamped to 0.5-200.
- Unparseable or empty model output is discarded - never rendered.

**Resilience:**

- 20s provider timeout (linked `CancellationTokenSource`), two attempts, then the deterministic fallback.
- Fallback output is derived only from the user's own text (sentence split, title = first 12 words, keyword-based priority),
  so it never fabricates external systems.
- Timeouts caused by client disconnects surface as HTTP 504; rate-limit breaches as 429.

**Audit trail:**

- Successful model calls are written to `AiInteractionLogs` with caller identity, model, full prompt, raw output and latency.
- Local/fallback suggestions are not logged, and a logging failure never fails the user request.

**Secrets & data handling:**

- API key is read from user-secrets (`AI:GoogleAIStudio:ApiKey`), never from source control.
- Only the task description, title and project context are sent - no user identifiers, tokens or PII.
- Prompt-injection mitigation: the system prompt pins the output schema and forbids extra prose; anything outside the
  schema is dropped during parsing, and the model output is never executed or interpolated into SQL.
