# dep-test-1

Monorepo with a Next.js frontend in `fe` and an ASP.NET Core controller API in `be`. The backend uses PostgreSQL, Redis, EF Core, ASP.NET Core Identity, and Redis-backed session authentication.

## Run from the root

```sh
pnpm dev:db
```

Starts local PostgreSQL at `localhost:5432` and Redis at `localhost:6379`.

```sh
pnpm dev:be
```

Starts the API at `http://localhost:5000`.

```sh
pnpm dev:fe
```

Starts the frontend at `http://localhost:3000`.

## Useful URLs

- Frontend: `http://localhost:3000`
- Login page: `http://localhost:3000/login`
- Dashboard page: `http://localhost:3000/dashboard`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- API login endpoint: `http://localhost:5000/api/auth/login`
- API logout endpoint: `http://localhost:5000/api/auth/logout`
- API current user endpoint: `http://localhost:5000/api/auth/me`
- API admin check endpoint: `http://localhost:5000/api/auth/admin-check`
- API projects endpoint: `http://localhost:5000/api/projects`
- API project tasks endpoint: `http://localhost:5000/api/project-tasks`
- API project task joins endpoint: `http://localhost:5000/api/projects/task-joins`
- API announcements endpoint: `http://localhost:5000/api/announcements`
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI document: `http://localhost:5000/openapi/v1.json`

## Local auth users

Development seed data creates these ASP.NET Core Identity users and roles:

```text
admin@example.local / Admin123!
member@example.local / Member123!
```

Use `POST /api/auth/login` to create a Redis-backed session, then send it as:

```text
X-Session-Id: <session-id>
```

The backend supports multiple Identity roles per user. The frontend displays one primary role to keep the dashboard clear.

## Frontend variables

The frontend uses Auth.js as a cookie-backed BFF. Browser code calls the
Next.js API routes, and those routes call the ASP.NET API with `X-Session-Id`.

```text
BACKEND_API_URL=http://localhost:5000
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=<at-least-32-byte-random-secret>
```

`BACKEND_API_URL` defaults to `http://localhost:5000` for local development.
Sentry is disabled outside production builds and also stays disabled when no
DSN is configured. To enable frontend error reporting in production, set:

```text
SENTRY_DSN=<server-side-sentry-dsn>
NEXT_PUBLIC_SENTRY_DSN=<browser-sentry-dsn>
NEXT_PUBLIC_SENTRY_ENVIRONMENT=production
```

Source-map upload is disabled in `fe/next.config.ts`; enable it separately only
after approving the `@sentry/cli` dependency build script.

## Railway backend variables

Set these on the `be` service:

```text
ALLOWED_ORIGINS=https://<frontend-domain>.up.railway.app
DATABASE_URL=${{Postgres.DATABASE_URL}}
REDIS_URL=${{Redis.REDIS_URL}}
AuthSession__IdleTimeoutMinutes=120
AuthSession__AbsoluteExpirationDays=7
AuthSession__RememberMeAbsoluteExpirationDays=14
SENTRY_DSN=<backend-sentry-dsn>
```

For this demo, set the backend pre-deploy command to:

```text
dotnet be.dll migrate-and-seed
```

## Dependency checks

Run installs from the repo root so pnpm creates one root lockfile:

```sh
pnpm install
```

The workspace pins direct dependency versions, delays newly published npm packages by 24 hours, blocks exotic transitive package sources, and fails on unreviewed dependency build scripts.

After installing, run:

```sh
pnpm audit
dotnet list be/be.csproj package --vulnerable --include-transitive
```

Update the local .NET 10 runtime/SDK to `10.0.7` or newer before running the API.
