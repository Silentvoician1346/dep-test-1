# dep-test-1

Monorepo with a Next.js frontend in `fe` and an ASP.NET Core controller API in `be`. The backend uses PostgreSQL, EF Core, ASP.NET Core Identity, and JWT bearer authentication.

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
- API login endpoint: `http://localhost:5000/api/auth/login`
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

Use `POST /api/auth/login` to get a bearer token, then send it as:

```text
Authorization: Bearer <access-token>
```

The backend supports multiple Identity roles per user. The frontend displays one primary role to keep the dashboard clear.

## Frontend variables

The frontend uses Auth.js as a cookie-backed BFF. Browser code calls the
Next.js API routes, and those routes call the ASP.NET API with the backend JWT.

```text
BACKEND_API_URL=http://localhost:5000
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=<at-least-32-byte-random-secret>
```

`BACKEND_API_URL` defaults to `http://localhost:5000` for local development.

## Railway backend variables

Set these on the `be` service:

```text
ALLOWED_ORIGINS=https://<frontend-domain>.up.railway.app
Jwt__Issuer=dep-test-1
Jwt__Audience=dep-test-1-api
Jwt__SigningKey=<at-least-32-byte-random-secret>
DATABASE_URL=${{Postgres.DATABASE_URL}}
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
