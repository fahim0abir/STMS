# SmartTaskUi

Angular 22 standalone front-end for the SmartTask management platform (projects, tasks, dashboard, AI-assisted descriptions).

## Features

- **Responsive shell** — collapsible sidebar + topbar, role-aware navigation, user profile, logout.
- **Dashboard** — KPI cards (projects, total/completed/pending/upcoming tasks), tasks-by-status and tasks-by-priority breakdowns, completed-vs-pending ring, upcoming-due list.
- **Projects** — server-side pagination, sorting, 300 ms debounced keyword search, create/edit modal (Admin + ProjectManager), delete (Admin only).
- **Tasks** — status/priority filter chips, due-date range filters, "assigned to me" filter, sorting, pagination, create/edit modal with **Improve with AI** (accept / reject / revert).
- **Auth** — JWT access token + refresh token, typed `User` persisted in `localStorage`, route guards and RBAC (`Admin`, `ProjectManager`, `TeamMember`).

## Architecture

```
src/app/
  core/            api.config.ts, auth (AuthService, TokenService), guards, interceptors, services
  shared/          models (DTO mirrors, enums, PagedResult, ApiResponse), ui primitives, layout shell
  features/        auth, dashboard, projects, tasks (containers + presentational components)
```

- Standalone components + lazy-loaded feature routes; `ShellComponent` is the authenticated layout route.
- All lists consume `PagedResult<T>` from `ApiResponse<T>` and unsubscribe via `takeUntilDestroyed()`.
- UI is built without a component library; primitives live in `shared/ui` (button, badge, card, spinner, pagination, search/sort bar).

## Backend contract

Base URL: `http://localhost:5195/api` (see `src/app/core/api.config.ts`). Endpoints used:

| Area | Endpoint |
| --- | --- |
| Auth | `POST /auth/login`, `POST /auth/register`, `POST /auth/refresh-token` |
| Projects | `GET /projects/paged`, `POST /projects`, `PUT /projects/{id}`, `DELETE /projects/{id}` |
| Tasks | `GET /taskitems/paged`, `POST /taskitems?projectId=`, `PUT /taskitems/{id}`, `DELETE /taskitems/{id}` |
| AI | `POST /tasks/enhance-description` |
| Dashboard | `GET /dashboard/stats` |

Enums are serialized as numbers by the API; the client mirrors them in `shared/models/enums.ts` and normalizes responses.

## Setup

```bash
npm install
ng serve          # http://localhost:4200
ng build          # production build into dist/
```

Start the ASP.NET Core API first (`STMS/Server`). CORS is open (`AllowAll`) in development.

## Roles

| Capability | Admin | ProjectManager | TeamMember |
| --- | --- | --- | --- |
| Dashboard | yes | yes | yes |
| Tasks (view, filter) | yes | yes | yes |
| Projects list | yes | yes | no (guarded route) |
| Create/edit projects & tasks | yes | yes | no |
| Delete projects & tasks | yes | no | no |

## Notes

- The task modal requires a project when creating a task (`POST /taskitems?projectId=`).
- Assignee selection is not exposed in the UI (the API has no user-directory endpoint); the "Assigned to me" filter uses the current user's id.
- AI enhancement is validated client-side (5–1,000 characters), times out after 20 s, and falls back to the original description on failure.
