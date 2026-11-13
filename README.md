# Smart Task Management System (STMS)

Full-stack project & task management application built for the **Software Engineer || .NET Full Stack** assignment.
ASP.NET Core 9 Web API (Clean Architecture, EF Core code-first, SQL Server) + Angular 18+ standalone frontend,
JWT with refresh-token rotation, role-based access control, server-side paging/sorting/filtering, a KPI dashboard
and an AI endpoint that rewrites rough task notes into structured, actionable task definitions.

## Table of Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Folder Structure](#folder-structure)
- [Getting Started](#getting-started)
- [Configuration & Secrets](#configuration--secrets)
- [Database (Code-First / SQL Server)](#database-code-first--sql-server)
- [API Overview](#api-overview)
- [Role Permissions](#role-permissions)
- [AI Feature](#ai-feature)
- [Seeded Users](#seeded-users)
- [Deliverables](#deliverables)

## Features

- **Authentication**: register, login, refresh-token rotation, logout; BCrypt password hashing; JWT bearer tokens.
- **Authorization**: `Admin`, `ProjectManager`, `TeamMember` roles enforced by API policies and Angular route guards.
- **Projects**: create, update, delete, details, list, keyword search, sorting, pagination.
- **Tasks**: create, update, delete, assign, status, priority, due date, keyword search, sorting, pagination.
- **Task vocabulary** (per assignment): `To Do`, `In Progress`, `Completed`, `Cancelled`; priority `Low`, `Medium`, `High`, `Critical`.
- **Dashboard**: total projects, total tasks, tasks by status, tasks by priority, completed vs pending, upcoming due tasks.
- **AI**: `POST /api/tasks/enhance-description` returns a structured task definition (improved description, suggested title,
  acceptance criteria, subtasks, suggested priority, hour estimate) with a deterministic local fallback.
- **Cross-cutting**: `ApiResponse<T>` envelope, global exception middleware, FluentValidation, Serilog (console + rolling file),
  fixed-window rate limiting, CORS, HTTPS redirection, ASP.NET health checks (`/health`).

## Technology Stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 9 Web API, Clean Architecture (API / Application / Infrastructure / Domain) |
| Data | EF Core 9 code-first migrations, SQL Server (LocalDB or full instance) |
| Security | JWT bearer + refresh tokens, BCrypt, role-based policies, FluentValidation, rate limiting |
| Observability | Serilog console + rolling file logs, `AddHealthChecks()` with DbContext check |
| AI | GitHub Models chat completions (`gpt-4o-mini`) over `HttpClient`, JSON-mode output |
| Frontend | Angular (standalone components), TypeScript, SCSS, RxJS, typed reactive forms |
| Docs | Swagger / OpenAPI, PROMPTS.md, idempotent SQL script |

## Architecture

```
Angular (Client)
    |  HTTPS + JWT bearer
    v
SmartTask.API            controllers, middleware, Program.cs, auth policies, rate limiting
    v
SmartTask.Application    DTOs, service interfaces, FluentValidation validators
    v
SmartTask.Infrastructure EF Core DbContext, repositories, migrations, auth + AI services, seeder
    v
SmartTask.Domain         entities (User, Project, TaskItem, RefreshToken) and enums
```

Patterns applied: Clean Architecture layering, repository abstraction, DI with interfaces, DTO/entity separation,
middleware pipeline, options-based configuration, strategy-style fallback for external AI calls.

## Folder Structure

```
STMS/
  Client/                            # Angular standalone application
    src/app/
      core/                          # api.config, auth services, guards, interceptors, HTTP services
      features/                      # auth, dashboard, projects, tasks (containers + presentational)
      shared/                        # layout, models, ui components, validators
  Server/
    SmartTask.API/                   # controllers, middlewares, Program.cs, appsettings
    SmartTask.Application/           # DTOs, interfaces, validators
    SmartTask.Domain/                # entities and enums
    SmartTask.Infrastructure/        # DbContext, migrations, repositories, services, seeder
  docs/
    swagger.json                     # exported OpenAPI document (single API collection source of truth)
    database.sql                     # idempotent EF Core migration script
  PROMPTS.md                         # AI prompt design, examples, validation and safety
  README.md
```

## Getting Started

Prerequisites: .NET SDK 9, Node.js 20+, SQL Server (LocalDB is enough) and the EF CLI (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Backend
cd Server
dotnet restore SmartTask.slnx
dotnet ef database update --project SmartTask.Infrastructure --startup-project SmartTask.API
dotnet run --project SmartTask.API          # https/http://localhost:5195, Swagger at /swagger

# 2. Frontend
cd Client
npm install
npm start                                   # http://localhost:4200
```

The API seeds roles and three demo users on first start, then serves Swagger UI in Development at
`http://localhost:5195/swagger`.

## Configuration & Secrets

Never commit real secrets. `Server/SmartTask.API/appsettings.json` holds non-secret defaults
(connection string + issuer/audience); keys live in user-secrets:

```bash
cd Server/SmartTask.API
dotnet user-secrets set "Jwt:Key" "<random 32+ character key>"
dotnet user-secrets set "AI:GitHubModels:ApiKey" "<github models PAT>"
```

Optional AI overrides (defaults shown):

| Key | Default | Purpose |
| --- | --- | --- |
| `AI:GitHubModels:Endpoint` | `https://models.inference.ai.azure.com/chat/completions` | Chat-completions endpoint |
| `AI:GitHubModels:Model` | `gpt-4o-mini` | Model name |
| `AI:GitHubModels:Temperature` | `0.4` | Determinism (0-1) |

`appsettings.Example.json` documents the full key layout without values.

## Database (Code-First / SQL Server)

- Provider: `Microsoft.EntityFrameworkCore.SqlServer`, database `SmartTaskDb`.
- Default connection: `Server=(localdb)\mssqllocaldb;Database=SmartTaskDb;Trusted_Connection=True;MultipleActiveResultSets=true`.
  Swap in a full instance via `ConnectionStrings:DefaultConnection` (user-secrets or environment variable).
- Migrations live in `Server/SmartTask.Infrastructure/Migrations`:
  - `InitialCreate` - schema for `Users`, `Projects`, `TaskItems`, `RefreshTokens`.
  - `AlignTaskStatusVocabulary` - data migration remapping legacy statuses to the assignment vocabulary.
- Apply: `dotnet ef database update --project SmartTask.Infrastructure --startup-project SmartTask.API`.
- Portable script: `docs/database.sql` is regenerated with
  `dotnet ef migrations script --idempotent -o ../docs/database.sql` and is safe to re-run.

## API Overview

All responses use the `ApiResponse<T>` envelope: `{ success, message, data, statusCode }`.

| Area | Method & Route | Notes |
| --- | --- | --- |
| Auth | `POST /api/auth/register` | Creates a user (default `TeamMember`) |
| Auth | `POST /api/auth/login` | Returns access + refresh token |
| Auth | `POST /api/auth/refresh-token` | Rotates the refresh token |
| Auth | `POST /api/auth/logout` | Revokes the refresh token |
| Projects | `GET /api/projects/paged` | `keyword, sortBy, sortOrder, pageNumber, pageSize` |
| Projects | `GET /api/projects/{id}` | Project details |
| Projects | `POST /api/projects` | Admin / ProjectManager |
| Projects | `PUT /api/projects/{id}` | Admin / ProjectManager |
| Projects | `DELETE /api/projects/{id}` | Admin |
| Tasks | `GET /api/taskitems/paged` | `keyword, status, priority, assigneeId, dueFrom, dueTo, sortBy, sortOrder, page` |
| Tasks | `GET /api/taskitems/{id}` | Task details |
| Tasks | `GET /api/taskitems?projectId=` | Tasks of one project |
| Tasks | `POST /api/taskitems?projectId=` | Admin / ProjectManager |
| Tasks | `PUT /api/taskitems/{id}` | Admin / ProjectManager |
| Tasks | `PATCH /api/taskitems/{id}/status` | Assignee, Admin or ProjectManager |
| Tasks | `DELETE /api/taskitems/{id}` | Admin |
| Dashboard | `GET /api/dashboard/stats` | Totals, breakdowns, completion rate, upcoming due |
| AI | `POST /api/tasks/enhance-description` | Structured AI task improvement (rate limited) |
| Health | `GET /health` | ASP.NET health checks incl. database probe |

`docs/swagger.json` is the single exported API collection (Swagger/OpenAPI); import it into Postman or open it in
Swagger UI. The previous hand-maintained Postman collection was removed so the contract has one source of truth.

## Role Permissions

| Capability | Admin | ProjectManager | TeamMember |
| --- | --- | --- | --- |
| Register / login / refresh / logout | Yes | Yes | Yes |
| View projects & tasks | All | Owned projects | Assigned tasks |
| Create / update projects | Yes | Yes | No |
| Delete projects | Yes | No | No |
| Create / update / assign tasks | Yes | Yes | No |
| Delete tasks | Yes | No | No |
| Update own task status | Yes | Yes | Yes |
| Dashboard | Yes | Yes | Yes |
| AI enhance description | Yes | Yes | Yes |

## AI Feature

`POST /api/tasks/enhance-description` (policy `enhance`, 10 requests/minute) accepts:

```json
{ "description": "login page keeps failing when password has spaces", "title": "Fix login", "context": "SmartTask auth module" }
```

and returns an `ApiResponse<EnhanceDescriptionResponseDto>`:

```json
{
  "success": true,
  "data": {
    "enhancedDescription": "...",
    "suggestedTitle": "Fix login failures for passwords containing spaces",
    "acceptanceCriteria": ["..."],
    "subtasks": ["..."],
    "suggestedPriority": 2,
    "estimatedHours": 3.5,
    "model": "gpt-4o-mini",
    "usedFallback": false
  }
}
```

Input rules: 20-4,000 characters, optional title (200) and context (500). Timeout 20s with one retry; if the provider
fails twice the service returns a deterministic local suggestion (`usedFallback: true`, `model: "fallback"`) so the UI
never breaks. Prompt design, examples and safety notes are documented in `PROMPTS.md`.

## Seeded Users

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@smarttask.dev` | `Admin123!` |
| ProjectManager | `projectmanager@smarttask.dev` | `Pm123456!` |
| TeamMember | `teammember@smarttask.dev` | `Tm123456!` |

## Deliverables

- Source code: this repository (`Server/`, `Client/`, `docs/`).
- Database: EF Core code-first migrations + `docs/database.sql` (idempotent).
- Documentation: `README.md`, `PROMPTS.md`.
- API collection: `docs/swagger.json` (OpenAPI export, single source of truth).
