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
```

Railway provides `PORT`; the API binds to `0.0.0.0:$PORT`.

Useful URLs after deploy:

```text
https://<backend-domain>.up.railway.app/api/message
https://<backend-domain>.up.railway.app/swagger
```

## Frontend service

Create another Railway service from the same GitHub repo.

In service settings:

- Service name: `fe`
- Root directory: leave unset, or set it to `/`
- Dockerfile path variable: `RAILWAY_DOCKERFILE_PATH=fe/Dockerfile`
- Watch paths:
  - `/fe/**`
  - `/package.json`
  - `/pnpm-lock.yaml`
  - `/pnpm-workspace.yaml`

Variables:

```text
NEXT_PUBLIC_API_URL=https://<backend-domain>.up.railway.app
```

Generate a public domain after the first successful deploy.

Do not set the frontend root directory to `/fe` when using `fe/Dockerfile`.
That Dockerfile intentionally builds from the repo root so it can use the root
`pnpm-lock.yaml` and `pnpm-workspace.yaml`.

## Preview deployments

Start without PR environments until production deploys work.

When ready, enable Railway PR Environments and Focused PR Environments:

- Frontend watch paths: `/fe/**`, `/package.json`, `/pnpm-lock.yaml`, `/pnpm-workspace.yaml`
- Backend watch paths: `/be/**`, `/dep-test-1.slnx`

## CI

GitHub Actions runs one build job for both services:

- `pnpm install --frozen-lockfile`
- `pnpm lint:fe`
- `pnpm build:fe`
- `dotnet restore be/be.csproj`
- `dotnet build be/be.csproj --configuration Release --no-restore`

Playwright E2E can be added later to the same workflow after the frontend calls the backend.
