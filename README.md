# SmartTask Management System (STMS)

A full-stack task & project management application with JWT + refresh-token authentication, role-based access control, server-side paged/sorted/filtered lists, a KPI dashboard and AI-assisted task description enhancement.

## Features

- Auth: register, login, refresh-token rotation, logout (BCrypt password hashing).
- RBAC: `Admin`, `ProjectManager`, `TeamMember` enforced in API policies and Angular guards.
- Projects & tasks: CRUD with server-side paging, sorting, keyword and status/priority/due-date filters.
- Dashboard: totals, completion rate, breakdowns by status/priority, upcoming due tasks.
- AI: `POST /api/tasks/enhance-description` rewrites a task description into professional, actionable bullet points (GitHub Models `gpt-4o-mini`) with a deterministic fallback.
- Hardening: `ApiResponse<T>` envelope, global exception middleware, fixed-window rate limiting (`auth` 5/min, `enhance` 10/min), Serilog file + console logging, `/health` endpoint.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 9 Web API, Clean Architecture (API / Application / Infrastructure / Domain) |
| Data | EF Core 9 (code-first), SQL Server / LocalDB, FluentValidation, BCrypt |
| Frontend | Angular 22 standalone components, TypeScript, SCSS, RxJS, reactive forms |
| AI | GitHub Models chat completions (`gpt-4o-mini`) via `HttpClient` |
| Tooling | Swashbuckle (OpenAPI), Serilog, dotnet CLI, npm |

## Folder Structure

```
STMS/
├── Client/                       # Angular app
│   └── src/app/
│       ├── core/                 # api.config, auth services, guards, interceptors, HTTP services
│       ├── features/             # auth, dashboard, projects, tasks (containers + presentational)
│       └── shared/               # layout, models, ui components, validators
├── Server/
│   ├── SmartTask.API/            # controllers, middleware, Program.cs, appsettings.Example.json
│   ├── SmartTask.Application/    # DTOs, interfaces, FluentValidation validators
│   ├── SmartTask.Infrastructure/ # EF Core DbContext, migrations, repositories, services, AI client
│   └── SmartTask.Domain/         # entities and enums
├── docs/
│   ├── api-collection.json       # Postman v2.1 collection
│   ├── swagger.json              # exported OpenAPI document
│   └── database.sql              # idempotent schema script
├── PROMPTS.md                    # AI prompt design & safety notes
└── README.md
```

## Prerequisites (Windows)

- .NET SDK **9.0.312** (`Server/global.json` pins it, `rollForward: disable`).
- SQL Server LocalDB (`sqllocaldb info MSSQLLocalDB` should list an instance).
- Node.js **20+** and npm (repo built with Node 24 / npm 12).
- EF CLI: `dotnet tool install --global dotnet-ef`

## Setup

### 1. Backend

```powershell
cd Server
Copy-Item SmartTask.API\appsettings.Example.json SmartTask.API\appsettings.json
```

Configure secrets (never committed):

```powershell
cd SmartTask.API
dotnet user-secrets set "Jwt:Key" "<32+ character random string>"
dotnet user-secrets set "AI:GitHubModels:ApiKey" "<github-models-token>"
```

Create the database and run the API:

```powershell
cd ..   # Server
dotnet ef database update --project SmartTask.Infrastructure --startup-project SmartTask.API
dotnet run --project SmartTask.API --launch-profile http
```

API: `http://localhost:5195` · OpenAPI: `http://localhost:5195/swagger/v1/swagger.json` · Health: `http://localhost:5195/health`.
If `appsettings.json` already exists locally, skip the copy step — it is git-ignored.

### 2. Frontend

```powershell
cd Client
npm install
npm start
```

Open `http://localhost:4200`. The API base URL lives in `Client/src/app/core/api.config.ts` (`http://localhost:5195/api`).

## Seeded Test Credentials

Seeded on first startup by `ApplicationDbContextSeeder` (only when the `Users` table is empty).

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@smarttask.dev` | `Admin123!` |
| ProjectManager | `projectmanager@smarttask.dev` | `Pm123456!` |
| TeamMember | `teammember@smarttask.dev` | `Tm123456!` |

## API Reference

- Import `docs/api-collection.json` into Postman; `baseUrl` defaults to `http://localhost:5195`.
- `Auth > Login` auto-stores `accessToken` / `refreshToken` as collection variables; every other request inherits the bearer token.
- Raw OpenAPI document: `docs/swagger.json` (regenerate with the API running: `Invoke-WebRequest http://localhost:5195/swagger/v1/swagger.json -OutFile docs/swagger.json`).
- Database script: `docs/database.sql`.

Role rules: creating/updating projects and tasks requires `Admin` or `ProjectManager`; deleting requires `Admin`; all read endpoints require an authenticated user.

## Build & Test Commands

| Where | Command | Purpose |
| --- | --- | --- |
| `Client` | `npm start` / `npm run build` | dev server / production build |
| `Server` | `dotnet build` | compile all projects |
| `Server` | `dotnet ef migrations add <Name> --project SmartTask.Infrastructure --startup-project SmartTask.API` | add migration |

See [PROMPTS.md](PROMPTS.md) for the AI prompt design, validation and rate-limiting details.

