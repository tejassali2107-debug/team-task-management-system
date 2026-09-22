# TaskFlow — Team Task Management System

A role-based full-stack task management application for organizations to manage teams, assign tasks, track progress, collaborate through comments, and get notified of important task events.

**Live demo**: https://frontend-inky-nine-90.vercel.app (frontend on Vercel, backend on Render, database on Render PostgreSQL) — fully functional, seeded with the demo accounts below.

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API (C#), Clean Architecture (Domain / Application / Infrastructure / Api) |
| Database | SQL Server (Entity Framework Core, code-first migrations) |
| Auth | JWT bearer tokens, ASP.NET Core Identity password hasher, role-based authorization |
| Frontend | React 19 + Vite, React Router, Axios |
| Testing | xUnit (unit tests with EF Core InMemory) + integration tests (`WebApplicationFactory`) |
| API docs | Swagger / OpenAPI (built in) + Postman collection |
| DevOps | Docker multi-container setup (SQL Server + API + frontend via Nginx), GitHub Actions CI |

## Features

- JWT-based authentication with expiry handling; secure password hashing (no plaintext storage).
- Role-based access control for **Admin**, **Manager**, and **User**, enforced both at the API (authorization policies + per-request scoping in services) and in the UI (route guards, conditional actions).
- Team management: Admins create teams and assign managers; Admins and Managers add/remove team members.
- Task lifecycle: creation, assignment, status tracking (`To Do` → `In Progress` → `Done`), priority, due dates.
- Comment thread per task for collaboration.
- Notifications on task assignment and status changes. Delivered as in-app notifications (bell icon, polled) and **mock emails** — every notification is logged and persisted to an `EmailLogs` table, so delivery is fully auditable without needing a real SMTP provider.
- Dashboard with task-status overview and filtering by status, priority, and deadline.

## Role capabilities

| Role | Capabilities |
|---|---|
| Admin | Manage all teams and users; assign tasks to managers and users; full visibility |
| Manager | Create/assign tasks to their own team's members; manage their team's membership; manage users' assigned team via a Team-scoped view |
| User | View and update the status of tasks assigned to them; comment on their tasks |

## Project structure

```
backend/
  src/
    TaskManagement.Domain          # Entities, enums — no dependencies
    TaskManagement.Application     # DTOs, service interfaces, Result<T>
    TaskManagement.Infrastructure  # EF Core DbContext, migrations, service implementations, JWT, mock email
    TaskManagement.Api             # Controllers, Program.cs, Swagger, auth wiring
  tests/
    TaskManagement.UnitTests        # Service-level unit tests (RBAC logic, auth, notifications)
    TaskManagement.IntegrationTests # Full HTTP pipeline tests via WebApplicationFactory
frontend/
  src/
    api/         # Axios client + per-resource API modules
    context/     # AuthContext (token/user state, session-expiry handling)
    components/  # Reusable UI (Layout, modals, badges, notification bell)
    pages/       # Route-level pages (Dashboard, Tasks, Team, Users, Login...)
docs/
  swagger.json             # Exported OpenAPI spec
  postman_collection.json  # Postman collection covering every endpoint
docker-compose.yml
.github/workflows/ci.yml
```

## Getting started (local, without Docker)

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- SQL Server (Express/Developer/LocalDB — anything reachable via a connection string)

### 1. Backend

```bash
cd backend
# Point ConnectionStrings:DefaultConnection in src/TaskManagement.Api/appsettings.Development.json
# at your SQL Server instance (a Windows-auth localhost instance is configured by default).
dotnet restore
dotnet run --project src/TaskManagement.Api
```

The API applies EF Core migrations and seeds demo data automatically on first run. It listens on the URL printed in the console (e.g. `http://localhost:5080`) and serves Swagger UI at `/swagger`.

### 2. Frontend

```bash
cd frontend
cp .env.example .env.development   # adjust VITE_API_BASE_URL if your API runs on a different port
npm install
npm run dev
```

Open the printed local URL (default `http://localhost:5173`).

### 3. Run tests

```bash
cd backend
dotnet test TaskManagement.sln
```

## Getting started (Docker)

```bash
docker compose up --build
```

This starts three containers:

- `sqlserver` — SQL Server 2022, port `1433`
- `backend` — the API, port `8080` (migrates + seeds the database on startup)
- `frontend` — the React app served by Nginx, port `3000`

Visit `http://localhost:3000`. Override the default SA password / JWT signing key by setting `SA_PASSWORD` and `JWT_KEY` environment variables before running `docker compose up` — **do not use the default values for anything beyond local evaluation.**

## Sample credentials (seeded on first run)

| Role | Email | Password |
|---|---|---|
| Admin | `admin@taskflow.com` | `Admin@123` |
| Manager | `manager@taskflow.com` | `Manager@123` |
| User | `alice@taskflow.com` | `User@123` |
| User | `bob@taskflow.com` | `User@123` |

All seeded users belong to a demo "Engineering" team with a handful of sample tasks across all three statuses.

## API documentation

- **Swagger UI**: run the API and visit `/swagger` (interactive, includes the bearer-token auth flow).
- **Exported OpenAPI spec**: [`docs/swagger.json`](docs/swagger.json).
- **Postman collection**: [`docs/postman_collection.json`](docs/postman_collection.json) — import it, set the `baseUrl` variable, run "Login (Admin)" (auto-captures the token into a collection variable), then exercise the rest of the endpoints.

## Notifications

Task assignment and status-change events trigger:
1. An in-app `Notification` row for the affected user(s), visible via the bell icon in the header (polled every 30s).
2. A "mock email" — logged to the console and persisted in the `EmailLogs` table — standing in for a real SMTP integration. Swap `IEmailSender`'s registered implementation in `TaskManagement.Infrastructure/DependencyInjection.cs` for a real provider (e.g. SendGrid, SMTP) without touching any calling code.

## Configuration reference

Backend (`backend/src/TaskManagement.Api/appsettings.json` / environment variables):

| Key | Purpose |
|---|---|
| `Database:Provider` | `SqlServer` (default) or `Postgres` — selects the EF Core provider |
| `ConnectionStrings:DefaultConnection` | Connection string matching whichever provider is selected |
| `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` | JWT signing configuration |
| `Cors:AllowedOrigins` | Origins allowed to call the API from the browser |

Frontend (`frontend/.env.development` or build-time `VITE_API_BASE_URL`):

| Key | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base URL of the backend API, e.g. `http://localhost:5080/api` |

## Deployment

Both pieces are live:

| Piece | Host | URL |
|---|---|---|
| Frontend | Vercel (project `tejassali/frontend`) | https://frontend-inky-nine-90.vercel.app |
| Backend API | Render (Docker web service) | https://taskflow-backend-9a65.onrender.com (Swagger at `/swagger`) |
| Database | Render PostgreSQL (free tier) | internal to the Render service |

**Why Postgres in production but SQL Server locally?** Render has no managed SQL Server, and SQL Server itself needs more RAM (~2GB) than free-tier compute anywhere offers. Rather than force a paid host, the backend supports both providers side by side — see [`TaskManagement.Infrastructure.Postgres`](backend/src/TaskManagement.Infrastructure.Postgres) for the Postgres-specific migrations, and `Database:Provider` in config (`SqlServer` default, `Postgres` on Render) for the switch. The Domain/Application/Api layers and all business logic are identical either way; only `DependencyInjection.cs` branches on the provider.

**Redeploying the frontend** after a change:

```bash
cd frontend
vercel --prod
```

**Redeploying the backend**: Render's `autoDeploy` is on, so any push to `main` that touches `backend/` triggers a fresh build automatically. To change config, update the service's environment variables in the Render dashboard (or via its API) and redeploy.

**Caveat**: Render's free Postgres instance expires 30 days after creation unless upgraded to a paid plan — fine for evaluation, not for a permanent deployment.

Other options if you want to self-host instead:
- [Railway](https://railway.app) — same Dockerfile-based approach as Render.
- Azure App Service + Azure SQL Database — the most natural fit for a SQL Server + .NET stack, but Azure requires a card on file even for its free tier.

After deploying the backend, also add its URL to `Cors:AllowedOrigins` (e.g. via a `Cors__AllowedOrigins__0` environment variable) so the deployed frontend is permitted to call it.

After deploying the backend, update the frontend's `VITE_API_BASE_URL` (and the backend's `Cors:AllowedOrigins`) to point at each other's live URLs, then redeploy.

## CI

`.github/workflows/ci.yml` runs on every push/PR to `main`: restores, builds, and tests the backend (`dotnet test`), and installs, lints, and builds the frontend (`npm run build`).
