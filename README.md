# dep-test-1

Monorepo with a Next.js frontend in `fe` and an ASP.NET Core controller API in `be`.

## Run from the root

```sh
pnpm dev:fe
```

Starts the frontend at `http://localhost:3000`.

```sh
pnpm dev:be
```

Starts the API at `http://localhost:5000`.

## Useful URLs

- Frontend: `http://localhost:3000`
- API message endpoint: `http://localhost:5000/api/message`
- API sample endpoint: `http://localhost:5000/WeatherForecast`
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI document: `http://localhost:5000/openapi/v1.json`

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
