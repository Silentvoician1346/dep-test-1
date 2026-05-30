# Railway Deployment

This repo is intended to deploy as one Railway project with two services:

- `fe`: Next.js frontend
- `be`: ASP.NET Core API

## Prerequisites

- Push the repo to GitHub.
- Keep the root `pnpm-lock.yaml` committed.
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
Jwt__Issuer=dep-test-1
Jwt__Audience=dep-test-1-api
Jwt__SigningKey=<at-least-32-byte-random-secret>
DATABASE_URL=${{Postgres.DATABASE_URL}}
```

Set these variables on the backend service. `ALLOWED_ORIGINS` must be the
frontend origin only, with protocol and without a path.

Use the exact frontend origin in `ALLOWED_ORIGINS`. Do not include `/login`,
`/dashboard`, or a trailing path.

Generate `Jwt__SigningKey` with a long random value, for example:

```sh
openssl rand -base64 48
```

Set this pre-deploy command on the backend service:

```text
dotnet be.dll migrate-and-seed
```

Railway runs the pre-deploy command after building the image and before starting
the new deployment. The command applies EF Core migrations and creates demo seed
data before the API starts. Auth data is stored in the ASP.NET Core Identity
`AspNet*` tables.

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
NEXT_PUBLIC_API_URL=https://<backend-domain>.up.railway.app
```

Set `NEXT_PUBLIC_API_URL` before deploying the frontend. Next.js inlines
`NEXT_PUBLIC_*` variables into the browser bundle during `next build`, so adding
or changing this variable requires a frontend redeploy.

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
