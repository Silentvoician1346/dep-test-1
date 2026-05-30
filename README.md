# dep-test-1

Monorepo with a Next.js frontend in `fe` and an ASP.NET Core controller API in `be`.

## Run from the root

```sh
pnpm dev:db
```

Starts the local PostgreSQL database at `localhost:5432`.

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
- API message endpoint: `http://localhost:5000/api/message`
- API login endpoint: `http://localhost:5000/api/auth/login`
- API current user endpoint: `http://localhost:5000/api/auth/me`
- API admin check endpoint: `http://localhost:5000/api/auth/admin-check`
- API admin project task report endpoint: `http://localhost:5000/api/admin/project-task-report`
- API database overview endpoint: `http://localhost:5000/api/database-overview` requires admin bearer token
- API sample endpoint: `http://localhost:5000/WeatherForecast`
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI document: `http://localhost:5000/openapi/v1.json`

## Local auth users

Development seed data creates these users:

```text
admin@example.local / Admin123!
member@example.local / Member123!
```

Use `POST /api/auth/login` to get a bearer token, then send it as:

```text
Authorization: Bearer <access-token>
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
