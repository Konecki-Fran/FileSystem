# F24 Browser-Based File System

A small browser-based file system with folders, name-only files, recursive deletion, global filename-prefix search, and exact-name search in the current folder. The application uses a React frontend, an ASP.NET Core API, and PostgreSQL.

## Prerequisites

- Docker Desktop for the containerized options.
- .NET SDK 10 and Node.js 22 for IDE/local development.

## Configuration

Create a root `.env` file from the committed template before starting PostgreSQL or the full stack:

```powershell
Copy-Item .env.example .env
```

The committed example contains an intentionally non-secret development password. For a fresh local database, the API and PostgreSQL containers both receive `POSTGRES_PASSWORD`, so the values must match. The file is ignored by Git and is passed to containers at runtime; it is not copied into Docker images. `FRONTEND_PORT` is optional and defaults to `8080`.

Changing `POSTGRES_PASSWORD` after PostgreSQL has initialized its persistent volume does not change the database user's stored password. Either update the database credentials separately or remove the local volume with the cleanup command for the Compose configuration that started it, as described below.

For production, use a unique secret supplied by the deployment environment or a secret manager; do not use the development value from `.env.example`.

## Run the complete application with Docker

```powershell
docker compose up --build
```

Open `http://localhost:8080`, or the value configured in `FRONTEND_PORT`.

The Compose stack starts PostgreSQL first, waits for it to become healthy, starts the API, then starts the Nginx-served frontend. The frontend proxies API requests to the API container, so no browser CORS configuration is required.

Stop the stack with:

```powershell
docker compose down
```

The PostgreSQL volume persists data. To remove it and reinitialize the schema, run the destructive command below:

```powershell
docker compose down -v
```

## Run the API and frontend from an IDE

Start only PostgreSQL in Docker:

```powershell
docker compose -f docker-compose.db.yml up -d
```

Stop the database-only environment with the same Compose file:

```powershell
docker compose -f docker-compose.db.yml down
```

To remove its persistent volume and reinitialize PostgreSQL with the current `.env` credentials and schema, run:

```powershell
docker compose -f docker-compose.db.yml down -v
docker compose -f docker-compose.db.yml up -d
```

The Compose file used for cleanup must match the one used to start the environment. Plain `docker compose down -v` targets the full stack in `docker-compose.yml`; it does not remove the `postgres` service created by `docker-compose.db.yml`. Mixing the commands can leave `filesystem-postgres-1` attached to the shared network and volume, causing Docker to report that those resources are still in use.

Run the API on the port used by the frontend development proxy:

```powershell
dotnet run --project backend/F24 --urls http://localhost:5000
```

In another terminal, start the frontend:

```powershell
cd frontend
npm ci
npm run dev
```

Open `http://localhost:5173`.

## Tests and checks

To run the complete test suite—including backend and frontend unit tests, PostgreSQL integration tests, and Playwright—run this command from the repository root:

```powershell
.\run-all-tests.ps1
```

The script creates an isolated PostgreSQL Compose project on port `54329`, runs every suite, and removes its container and volume afterward. The port can be overridden if necessary:

```powershell
.\run-all-tests.ps1 -PostgresPort 54330
```

Docker Desktop must be running. The script installs Playwright's Chromium browser if it is not already available.

Individual checks can also be run manually:

```powershell
# Backend
dotnet test backend/F24.sln
dotnet format backend/F24.sln --verify-no-changes

# Frontend
cd frontend
npm ci
npm run typecheck
npm test
npm run build
```

The Playwright E2E suite uses the seeded database data. Start a fresh test database, run the API on port 5000, then run the tests:

```powershell
docker compose -f docker-compose.db.yml -f docker-compose.test.yml down -v
docker compose -f docker-compose.db.yml -f docker-compose.test.yml up -d
dotnet run --project backend/F24 --urls http://localhost:5000

# In another terminal
cd frontend
npm run test:e2e
```

PostgreSQL integration tests run when `RUN_POSTGRES_INTEGRATION_TESTS=true`. They reset the configured database schema, so use only the disposable test database above. With that database running:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS = 'true'
dotnet test backend/F24.sln
```

## API documentation

The Postman collection is available at `docs/F24 File System API.postman_collection.json`. The functional specification is in `docs/F24 Specification.md`.

## CI

GitHub Actions runs backend formatting/build/tests, frontend type checking/tests/build, and Playwright E2E tests on pull requests and pushes to `main`.
