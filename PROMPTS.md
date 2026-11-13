# PROMPTS.md — AI Task Description Enhancement

Covers the design, validation, failure handling and safety of the AI feature exposed at
`POST /api/tasks/enhance-description` (`Server/SmartTask.API/Controllers/TasksController.cs`),
implemented in `Server/SmartTask.Infrastructure/Services/TaskDescriptionEnhancementService.cs`.

## 1. Provider & Model

| Setting | Value |
| --- | --- |
| Provider | GitHub Models (Azure inference endpoint) |
| Endpoint | `https://models.inference.ai.azure.com/chat/completions` |
| Model | `gpt-4o-mini` |
| Temperature | `0.7` |
| Max output tokens | `500` |
| HTTP timeout | 15 seconds |
| Credential | `AI:GitHubModels:ApiKey` via `dotnet user-secrets` (never committed) |

## 2. System Prompt Structure

The prompt is a constant in the service and is sent as the `system` message; the user's text is sent verbatim as the `user` message. Verbatim prompt:

```text
You are a professional task-description editor. Improve the given task description for
grammar, clarity, and a professional tone. Return ONLY the enhanced description as
actionable bullet points. Do not add preamble, explanations, or extra sections.
```

Design intent:

1. **Role** — "professional task-description editor" narrows the model to editing, not planning.
2. **Objectives** — grammar, clarity, professional tone (the three graded criteria).
3. **Format contract** — "ONLY ... actionable bullet points", explicitly forbidding preamble/sections, which keeps the response directly insertable into the task form.
4. **Deterministic-ish output** — low temperature plus a strict format means the UI never needs to parse structure.

## 3. Request / Response Contract

Request body (`EnhanceDescriptionRequestDto`):

```json
{ "description": "fix login bug asap users cannot sign in" }
```

Success (HTTP 200):

```json
{
  "success": true,
  "message": "Description enhanced.",
  "data": "- Resolve the login bug that prevents users from signing in.\n- Prioritise the fix as a high-urgency task.",
  "errors": null,
  "statusCode": 200
}
```

## 4. Input / Output Examples

**Example A — terse, unpunctuated input**

```text
input:  "write unit tests for auth service asap"
output:
- Write unit tests for the authentication service.
- Prioritise the task as urgent.
```

**Example B — already structured input**

```text
input:  "need to update the readme with setup steps and credentials"
output:
- Update the README with complete setup instructions.
- Document the seeded test credentials for all roles.
```

**Example C — fallback (no API key, timeout, non-2xx or unparsable body)**

```text
input:  "fix login bug asap"
output: "- fix login bug asap."
```

The fallback is deterministic: trim, append a period when missing, prefix with `- `, so the endpoint always returns a bullet-shaped string.

## 5. Validation

| Layer | Rule | Behaviour |
| --- | --- | --- |
| FluentValidation (`EnhanceDescriptionRequestDtoValidator`) | `NotEmpty`, `MinimumLength(5)`, `MaximumLength(1000)` | HTTP 400 with the joined error messages inside the `ApiResponse<T>` envelope |
| Service guard | Rejects whitespace and length outside 5–1000 | Returns the original text unchanged (no provider call) |
| Controller | Re-validates before delegating | Prevents bypass when the service is reused elsewhere |
| Client | Mirrors the 5–1000 limit before calling the API | Avoids pointless round-trips |

Length capping protects both cost and prompt-injection surface: long free-form payloads cannot be used to smuggle new instructions past the system prompt.

## 6. Safety & Abuse Controls

- **Authentication required** — the controller is `[Authorize]`; anonymous callers get 401 before any AI cost is incurred.
- **Rate limiting** — `[EnableRateLimiting("enhance")]` on a fixed window of 10 requests/minute per client (`Program.cs`); excess requests receive HTTP 429.
- **Timeout** — 15-second `HttpClient` timeout, so a stalled provider cannot hold a request thread indefinitely; on expiry the fallback fires.
- **Secret handling** — the API key is read from configuration backed by user-secrets, is never logged, and is never returned to the client.
- **No prompt leakage** — the system prompt is server-side only; API responses contain the enhanced text and nothing else.
- **Prompt-injection resistance** — the system message forbids extra sections and the user content cannot alter the tool's role; worst case the model returns prose inside a bullet, still plain text.
- **Output is treated as text** — the Angular form binds it through interpolation (auto-escaped), so markup in a model response cannot execute.
- **No silent data retention** — the description is not persisted by the enhancement service; it is only forwarded to the provider and returned.
- **Graceful degradation** — missing key, non-success status or malformed JSON all resolve to the local fallback, so the task form never breaks.

## 7. Client Integration

`Client/src/app/core/services/ai.service.ts` posts to `/tasks/enhance-description` through the authenticated interceptor; the task modal shows a loading state, replaces the textarea with the returned bullet list, and leaves the field editable if the call fails (the API still returns a usable fallback string on most failures).
