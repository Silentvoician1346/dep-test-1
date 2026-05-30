# Project Flow Summary

This project is a small full-stack monorepo with three main parts:

- `fe`: a Next.js frontend.
- `be`: an ASP.NET Core backend API.
- `postgres`: a local PostgreSQL database for development.

The frontend runs in the browser and calls the backend directly through HTTP. The backend exposes API endpoints, handles CORS for the frontend origin, and reads/writes data through EF Core. The database is not called directly by the frontend.

## Frontend Flow

The frontend entry page is `fe/src/app/page.tsx`.

When the user opens the app:

1. `/` redirects to `/dashboard`.
2. `/dashboard` checks for a stored access token.
3. If no token exists, the user is redirected to `/login`.
4. If a token exists, the frontend calls `GET /api/auth/me`.
5. If the token is valid, the dashboard is shown.
6. If the token is invalid, it is cleared and the user is redirected to `/login`.

The dashboard has a button: `Show backend message`.

When the button is clicked, the frontend reads `NEXT_PUBLIC_API_URL` and sends:

```text
GET {NEXT_PUBLIC_API_URL}/api/message
```

If the request succeeds, the backend response is shown as a toast message. If the API URL is missing or the request fails, an error toast is shown.

The login page is available at:

```text
/login
```

It calls:

```text
POST /api/auth/login
```

On success, it stores the returned bearer token in browser storage and redirects to `/dashboard`.

## Backend Flow

The backend starts from `be/Program.cs`.

On startup:

1. ASP.NET Core registers controller support.
2. EF Core is configured with the PostgreSQL connection string.
3. CORS is configured so the frontend is allowed to call the API.
4. Swagger/OpenAPI is enabled.
5. In Development, EF Core applies database migrations and inserts local seed data.
6. Controllers are mapped as HTTP endpoints.

The main endpoint used by the frontend is:

```text
GET /api/message
```

This endpoint is defined in `be/Controllers/MessageController.cs` and returns:

```text
Hello from ASP.NET Core
```

There is also a sample endpoint:

```text
GET /WeatherForecast
```

The backend also has a database verification endpoint:

```text
GET /api/database-overview
```

This endpoint reads from PostgreSQL through EF Core and returns users, projects, project tasks, and announcements.
Because it includes user data, it requires an authenticated `admin` user.

## Authentication And Authorization Flow

Authentication is handled by the ASP.NET backend.

The auth endpoints are:

```text
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me
GET /api/auth/admin-check
```

`register` creates a new active `app_user` with the `member` role. `login` checks the submitted password against the stored password hash and returns a JWT bearer token.

Protected requests use:

```text
Authorization: Bearer <access-token>
```

The token only identifies the user. On each protected request, ASP.NET reloads the current user from `app_user`. If the user is inactive or missing, the request is rejected. The current database role is added to the request claims before authorization checks run.

This means authorization is still based on the database user row, not only on stale token data.

The admin-only check endpoint requires:

```text
role = admin
```

There is also an admin-only joined database report endpoint:

```text
GET /api/admin/project-task-report
```

It uses an explicit join across:

```text
app_user
  join project
  join project_task
```

The dashboard has a `Load project task report` button that calls this endpoint with the stored bearer token and renders the JSON response. Non-admin users cannot load the report.

Development seed users:

```text
admin@example.local / Admin123!
member@example.local / Member123!
```

## Database Flow

The database is PostgreSQL running in local Docker.

The initial schema has:

- `app_user`: user records for future authorization.
- `project`: related to `app_user`.
- `project_task`: related to `project`.
- `announcement`: unrelated standalone table.

Relationships:

```text
app_user
  -> project
  -> project_task

announcement
  unrelated to the other tables
```

ASP.NET uses EF Core to connect to PostgreSQL. The frontend does not connect to the database.

## Local Development Flow

Start the database:

```sh
pnpm dev:db
```

Start the backend:

```sh
pnpm dev:be
```

The backend runs at:

```text
http://localhost:5000
```

Start the frontend:

```sh
pnpm dev:fe
```

The frontend runs at:

```text
http://localhost:3000
```

For local development, the frontend should use:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Request Flow

```text
Browser
  -> Next.js frontend
  -> fetch("{NEXT_PUBLIC_API_URL}/api/message")
  -> ASP.NET Core backend
  -> MessageController
  -> "Hello from ASP.NET Core"
  -> frontend toast
```

Database-backed request:

```text
Browser
  -> Next.js frontend
  -> ASP.NET Core backend
  -> EF Core
  -> PostgreSQL
  -> EF Core
  -> ASP.NET Core response
  -> Browser
```

Authenticated request:

```text
Browser
  -> POST /api/auth/login
  -> ASP.NET checks app_user password hash
  -> ASP.NET returns JWT
  -> Browser sends Authorization: Bearer <token>
  -> ASP.NET validates token
  -> ASP.NET reloads app_user from PostgreSQL
  -> ASP.NET applies role-based authorization
  -> Controller action runs
```
