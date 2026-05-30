# Project Flow Summary

This project is a small full-stack monorepo with three main parts:

- `fe`: a Next.js frontend.
- `be`: an ASP.NET Core backend API.
- `postgres`: a PostgreSQL database.

The frontend talks only to the ASP.NET backend. The backend handles authentication, authorization, validation, and database access through EF Core.

## Frontend Flow

`/` redirects to `/dashboard`.

`/dashboard` checks for a stored access token. If no token exists, the user is redirected to `/login`. If a token exists, the frontend calls:

```text
GET /api/auth/me
```

If the token is valid, the dashboard is shown. If not, the token is cleared and the user is redirected to `/login`.

The login page calls:

```text
POST /api/auth/login
```

On success, it stores the bearer token and redirects to `/dashboard`.

## Backend Flow

The backend starts from `be/Program.cs`.

On startup:

1. ASP.NET Core registers controllers.
2. EF Core is configured with PostgreSQL.
3. ASP.NET Core Identity is configured for users, roles, claims, logins, and tokens.
4. JWT bearer authentication is configured.
5. CORS is configured for the frontend origin.
6. Swagger/OpenAPI is enabled.
7. Controllers are mapped as HTTP endpoints.

Railway can run this command before deployment:

```text
dotnet be.dll migrate-and-seed
```

That applies EF Core migrations and inserts demo seed data.

## Authentication And Authorization

Auth endpoints:

```text
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me
GET /api/auth/admin-check
```

Protected requests use:

```text
Authorization: Bearer <access-token>
```

Users and roles are stored in the standard ASP.NET Core Identity tables:

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetRoleClaims
AspNetUserLogins
AspNetUserTokens
```

JWT validation runs in ASP.NET authentication middleware. After the JWT is valid, the backend reloads the current user from `AspNetUsers`. If the user is missing or inactive, the request is rejected. All current Identity roles are added to claims before authorization runs.

One user can hold multiple roles through `AspNetUserRoles`. The backend uses all assigned roles for authorization, but `/api/auth/me` returns one primary display role so the frontend stays simple. Primary role priority is `admin`, then `member`, then any future role alphabetically.

`[Authorize]` on a controller or action requires a valid authenticated user before the endpoint code runs. `[Authorize(Policy = "AdminOnly")]` also requires the authenticated user to have an `admin` role claim. Row-level ownership is still enforced inside the project and task queries because `[Authorize]` does not know which database rows a user owns.

## API Surface

All non-auth data endpoints require login.

Projects:

```text
GET    /api/projects?page=1&pageSize=10
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}
```

Project tasks:

```text
GET    /api/project-tasks?page=1&pageSize=10
GET    /api/project-tasks/{id}
POST   /api/project-tasks
PUT    /api/project-tasks/{id}
DELETE /api/project-tasks/{id}
```

Project and task join read:

```text
GET /api/projects/task-joins?page=1&pageSize=10
```

Announcements:

```text
GET    /api/announcements?page=1&pageSize=10
GET    /api/announcements/{id}
POST   /api/announcements
PUT    /api/announcements/{id}
DELETE /api/announcements/{id}
```

Normal users only see and mutate their own projects and tasks. Admin users can see and mutate all projects and tasks. Announcements are authenticated-only read/write.

Paginated responses use:

```json
{
  "page": 1,
  "pageSize": 10,
  "totalItems": 42,
  "totalPages": 5,
  "items": []
}
```

## Database Flow

The schema has:

- `AspNetUsers`: Identity user records.
- `AspNetRoles` and `AspNetUserRoles`: Identity role records and user-role membership.
- `project`: owned by `AspNetUsers`.
- `project_task`: belongs to `project`.
- `announcement`: standalone table.

Relationships:

```text
AspNetUsers
  -> project
  -> project_task

announcement
  unrelated to the other tables
```

Deleting a project cascades to its project tasks.

## Local Development

Start the database:

```sh
pnpm dev:db
```

Start the backend:

```sh
pnpm dev:be
```

Start the frontend:

```sh
pnpm dev:fe
```

For local development, the frontend should use:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Development seed users:

```text
admin@example.local / Admin123!
member@example.local / Member123!
```
