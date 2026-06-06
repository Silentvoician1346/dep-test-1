# Railway Deployment

This repo is intended to deploy as one Railway project with these services:

- `fe`: Next.js frontend
- `be`: ASP.NET Core API
- PostgreSQL
- Redis

## Prerequisites

- Push the repo to GitHub.
- Keep the root `pnpm-lock.yaml` committed.
- Add Railway PostgreSQL and Redis services to the project.
- The frontend service should build from the repo root so pnpm can use the root workspace and lockfile.
- The backend service should use `/be` as its root directory so Railway detects `be/Dockerfile`.

## Backend service

Create a Railway service from the GitHub repo.

In service settings:

- Service name: `be`
- Root directory: `/be`
- Public Networking: Generate Domain

Variables:

```text
ALLOWED_ORIGINS=https://<frontend-domain>.up.railway.app
DATABASE_URL=${{Postgres.DATABASE_URL}}
REDIS_URL=${{Redis.REDIS_URL}}
AuthSession__IdleTimeoutMinutes=120
AuthSession__AbsoluteExpirationDays=7
AuthSession__RememberMeAbsoluteExpirationDays=14
```

Set these variables on the backend service. `ALLOWED_ORIGINS` must be the
frontend origin only, with protocol and without a path.

Use the exact frontend origin in `ALLOWED_ORIGINS`. Do not include `/login`,
`/dashboard`, or a trailing path.

Set this pre-deploy command on the backend service:

```text
dotnet be.dll migrate-and-seed
```

Railway runs the pre-deploy command after building the image and before starting
the new deployment. The command applies EF Core migrations and creates demo seed
data before the API starts. Auth data is stored in the ASP.NET Core Identity
`AspNet*` tables. Active sessions are stored in Redis and revoked on logout.

The demo seed creates these users:

```text
admin@example.local / Admin123!
member@example.local / Member123!
```

This is acceptable for a Railway demo service. Do not run demo seed data for a
real production app.

Railway provides `PORT`; the API binds to `0.0.0.0:$PORT`.

The backend Docker image defaults to:

```text
dotnet be.dll serve
```

The same image also supports:

```text
dotnet be.dll migrate
dotnet be.dll seed
dotnet be.dll migrate-and-seed
```

Useful URLs after deploy:

```text
https://<backend-domain>.up.railway.app/api/auth/login
https://<backend-domain>.up.railway.app/api/projects
https://<backend-domain>.up.railway.app/api/projects/task-joins
https://<backend-domain>.up.railway.app/api/announcements
https://<backend-domain>.up.railway.app/swagger
```

## Frontend service

Create another Railway service from the same GitHub repo.

In service settings:

- Service name: `fe`
- Root directory: leave unset, or set it to `/`
- Dockerfile path variable: `RAILWAY_DOCKERFILE_PATH=fe/Dockerfile`
- If Railway does not use the Dockerfile, set these explicitly:
  - Build command: `pnpm build:fe`
  - Start command: `pnpm start:fe`
- Watch paths:
  - `/fe/**`
  - `/package.json`
  - `/pnpm-lock.yaml`
  - `/pnpm-workspace.yaml`

Variables:

```text
BACKEND_API_URL=https://<backend-domain>.up.railway.app
NEXTAUTH_URL=https://<frontend-domain>.up.railway.app
NEXTAUTH_SECRET=<at-least-32-byte-random-secret>
SENTRY_DSN=<frontend-server-sentry-dsn>
NEXT_PUBLIC_SENTRY_DSN=<browser-sentry-dsn>
NEXT_PUBLIC_SENTRY_ENVIRONMENT=production
```

Set these as runtime variables on the frontend service. The browser calls the
Next.js API routes; only the Next.js server needs the backend URL and backend
session id.

`NEXT_PUBLIC_SENTRY_DSN` and `NEXT_PUBLIC_SENTRY_ENVIRONMENT` must be present
when the Docker image is built because Next.js inlines public browser variables
into the frontend bundle. `fe/Dockerfile` declares them as build arguments for
Railway's Docker builder.

Generate a public domain after the first successful deploy.

Do not set the frontend root directory to `/fe` when using `fe/Dockerfile`.
That Dockerfile intentionally builds from the repo root so it can use the root
`pnpm-lock.yaml` and `pnpm-workspace.yaml`.

## Preview deployments

Start without PR environments until production deploys work.

When ready, enable Railway PR Environments and Focused PR Environments:

- Frontend watch paths: `/fe/**`, `/package.json`, `/pnpm-lock.yaml`, `/pnpm-workspace.yaml`
- Backend watch paths: `/be/**`

## CI

GitHub Actions runs one build job for both services:

- `pnpm install --frozen-lockfile`
- `pnpm lint:fe`
- `pnpm build:fe`
- `dotnet restore be/be.csproj`
- `dotnet build be/be.csproj --configuration Release --no-restore`

Playwright E2E can be added later to the same workflow after the frontend calls the backend.
