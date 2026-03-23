# MSSQL Feature Workspace

This repository is organized around the MSSQL feature under [src/features/mssql](src/features/mssql). It includes a containerized local development environment, a Dapper-based console sample, an ASP.NET Core CRUD backend, and two different test layers.

If you are new to this workspace, start with [INSTALL.md](INSTALL.md). This README is intentionally feature-oriented and focuses on what exists inside the MSSQL feature, how the pieces fit together, and how to run them.

## Quick Start

Use this flow if you want to get the feature running with the least amount of setup friction:

1. Copy [.devcontainer/.env.example](.devcontainer/.env.example) to `.devcontainer/.env`
2. Replace `MSSQL_SA_PASSWORD` with your own strong password
3. Open the repository root in VS Code
4. Run `Dev Containers: Rebuild and Reopen in Container`
5. Inside the container, run `dotnet run --project src/features/mssql/MssqlConsoleSample.csproj`
6. Inside the container, run `dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj`

If you prefer VS Code tasks for the backend, this workspace also provides:

- `prepare-web-app`
- `build-web-app`
- `run-web-app`
- `test-web-api`

Important:

- Copying this repository into an existing Codespace or an already-running container does not automatically apply [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json).
- If `dotnet --list-runtimes` does not show .NET 8, you are not in the intended workspace container yet.
- If `getent hosts sqlserver` returns nothing, the SQL Server sidecar is not reachable from the current shell.

The repository does not ship with a usable default SQL password. Publicly tracked files contain the placeholder `MSSQL_SA_PASSWORD_CHANGE_ME` on purpose.

## Feature Overview

The MSSQL feature is split into a few distinct parts:

1. Development environment
2. Console sample
3. Web backend
4. Integration and API tests
5. Persistent SQL Server data folders

This is not a single application with one entry point. It is a feature workspace that demonstrates multiple ways of working with SQL Server in the same repository.

## Feature Structure

Primary feature root:

- [src/features/mssql](src/features/mssql)

Important sub-areas:

- [src/features/mssql/api](src/features/mssql/api): Dapper-based console sample data access
- [src/features/mssql/backend](src/features/mssql/backend): ASP.NET Core CRUD backend
- [src/features/mssql/tests](src/features/mssql/tests): console integration tests against real SQL Server
- [src/features/mssql/backend/tests](src/features/mssql/backend/tests): backend API tests
- [src/features/mssql/.sqlserver](src/features/mssql/.sqlserver): persisted SQL Server data, logs, and secrets folders

## Feature: Development Environment

The feature depends on a Dev Container plus a SQL Server sidecar container.

Container configuration files:

- [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json)
- [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml)

What the environment provides:

- A workspace container based on Ubuntu 24.04
- .NET 8 installed through the Dev Container feature
- A separate `sqlserver` container running SQL Server 2022
- Recommended VS Code extensions in [.vscode/extensions.json](.vscode/extensions.json)

The workspace container is where you run `dotnet build`, `dotnet run`, and `dotnet test`.
The SQL Server engine itself runs in the sidecar container, not inside your host OS.

## Feature: Console Sample

The console sample demonstrates direct SQL Server access through Dapper.

Relevant files:

- [src/features/mssql/MssqlConsoleSample.csproj](src/features/mssql/MssqlConsoleSample.csproj)
- [src/features/mssql/Program.cs](src/features/mssql/Program.cs)
- [src/features/mssql/api/DatabaseInitializer.cs](src/features/mssql/api/DatabaseInitializer.cs)
- [src/features/mssql/api/AuthorRepository.cs](src/features/mssql/api/AuthorRepository.cs)
- [src/features/mssql/types/Author.cs](src/features/mssql/types/Author.cs)
- [src/features/mssql/appsettings.json](src/features/mssql/appsettings.json)

What it does:

- Connects to SQL Server through Dapper
- Creates the sample database if needed
- Creates `dbo.Authors` if needed
- Seeds sample data
- Reads authors back and prints them to the console

Run it with:

```bash
dotnet run --project src/features/mssql/MssqlConsoleSample.csproj
```

## Feature: Web Backend

The backend exposes an Authors CRUD API and a minimal browser-based admin page.

Relevant files:

- [src/features/mssql/backend/MssqlCrudBackend.csproj](src/features/mssql/backend/MssqlCrudBackend.csproj)
- [src/features/mssql/backend/Program.cs](src/features/mssql/backend/Program.cs)
- [src/features/mssql/backend/appsettings.json](src/features/mssql/backend/appsettings.json)
- [src/features/mssql/backend/Features/Authors/AuthorEndpoints.cs](src/features/mssql/backend/Features/Authors/AuthorEndpoints.cs)
- [src/features/mssql/backend/Features/Authors/AuthorDatabaseBootstrapper.cs](src/features/mssql/backend/Features/Authors/AuthorDatabaseBootstrapper.cs)
- [src/features/mssql/backend/Features/Authors/SqlAuthorRepository.cs](src/features/mssql/backend/Features/Authors/SqlAuthorRepository.cs)
- [src/features/mssql/backend/Features/Authors/InMemoryAuthorRepository.cs](src/features/mssql/backend/Features/Authors/InMemoryAuthorRepository.cs)
- [src/features/mssql/backend/wwwroot/index.html](src/features/mssql/backend/wwwroot/index.html)

HTTP surface:

- `GET /api/health`
- `GET /api/authors/`
- `GET /api/authors/{id}`
- `POST /api/authors/`
- `PUT /api/authors/{id}`
- `DELETE /api/authors/{id}`
- `/` for the admin page
- `/swagger` for Swagger UI

Run it with:

```bash
dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj
```

Or run the provided VS Code task:

- `run-web-app`: starts the backend on `http://0.0.0.0:5000` and frees port `5000` first if needed

Useful local URLs after startup:

- `http://localhost:5000/`
- `http://localhost:5000/swagger`
- `http://localhost:5000/api/health`

Check `http://localhost:5000/api/health` after startup to confirm whether the backend is using `sqlserver` or `in-memory`.

## Feature: Repository Modes

The backend can run in two repository modes:

1. SQL Server mode
2. In-memory mode

This behavior is controlled in [src/features/mssql/backend/Program.cs](src/features/mssql/backend/Program.cs).

How it works:

1. The backend checks whether the environment is `Testing`
2. It reads `Authors:UseInMemoryRepository`
3. If SQL Server is expected, it tries to bootstrap the database
4. If bootstrap fails and fallback is enabled, it switches to the in-memory repository

Because of that, a successful backend startup does not automatically mean SQL Server is being used. The quickest check is `GET /api/health`.

## Feature: Tests

This feature has two separate test layers.

### Console Integration Tests

Relevant files:

- [src/features/mssql/tests/MssqlConsoleSample.Tests.csproj](src/features/mssql/tests/MssqlConsoleSample.Tests.csproj)
- [src/features/mssql/tests/AuthorRepositoryCrudTests.cs](src/features/mssql/tests/AuthorRepositoryCrudTests.cs)
- [src/features/mssql/tests/appsettings.json](src/features/mssql/tests/appsettings.json)

These tests hit a real SQL Server instance.

Run them with:

```bash
dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj
```

### Backend API Tests

Relevant files:

- [src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj](src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj)
- [src/features/mssql/backend/tests/AuthorApiCrudTests.cs](src/features/mssql/backend/tests/AuthorApiCrudTests.cs)
- [src/features/mssql/backend/tests/MssqlCrudBackendFactory.cs](src/features/mssql/backend/tests/MssqlCrudBackendFactory.cs)

These tests do not hit real SQL Server. They use ASP.NET Core `WebApplicationFactory` and replace the SQL repository with an in-memory repository in the `Testing` environment.

Run them with:

```bash
dotnet test src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj
```

## Feature: Configuration and Secrets

The main password override is:

- `MSSQL_SA_PASSWORD`

Tracked files intentionally use this placeholder:

- `MSSQL_SA_PASSWORD_CHANGE_ME`

That placeholder appears in public configuration to document intent without shipping a usable password.

Password-related files:

- [.devcontainer/.env.example](.devcontainer/.env.example)
- [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml)
- [src/features/mssql/appsettings.json](src/features/mssql/appsettings.json)
- [src/features/mssql/tests/appsettings.json](src/features/mssql/tests/appsettings.json)
- [src/features/mssql/backend/appsettings.json](src/features/mssql/backend/appsettings.json)

Connection string resolution is centralized in:

- [src/features/mssql/ConnectionString/SqlServerConnectionStringResolver.cs](src/features/mssql/ConnectionString/SqlServerConnectionStringResolver.cs)

If `MSSQL_SA_PASSWORD` exists in the environment, it overrides the password embedded in the tracked connection strings.

## Feature: Persistent SQL Server Data

The SQL Server sidecar persists its data inside the repository folder structure.

Mapped directories:

- [src/features/mssql/.sqlserver/data](src/features/mssql/.sqlserver/data)
- [src/features/mssql/.sqlserver/log](src/features/mssql/.sqlserver/log)
- [src/features/mssql/.sqlserver/secrets](src/features/mssql/.sqlserver/secrets)

Docker Compose maps them to:

```yaml
volumes:
   - ../src/features/mssql/.sqlserver/data:/var/opt/mssql/data
   - ../src/features/mssql/.sqlserver/log:/var/opt/mssql/log
   - ../src/features/mssql/.sqlserver/secrets:/var/opt/mssql/secrets
```

The engine runs in the SQL Server container. The files are persisted in the repository directory tree.

## Build and Run Commands

Console sample:

```bash
dotnet build src/features/mssql/MssqlConsoleSample.csproj
dotnet run --project src/features/mssql/MssqlConsoleSample.csproj
```

Console integration tests:

```bash
dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj
```

Web backend:

```bash
dotnet build src/features/mssql/backend/MssqlCrudBackend.csproj
dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj
```

VS Code tasks for the backend:

```text
prepare-web-app
build-web-app
run-web-app
test-web-api
```

The `run-web-app` task binds the backend to `http://0.0.0.0:5000` and stops any process already using port `5000` before launching the app.

Backend API tests:

```bash
dotnet test src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj
```

## Current Limitations

- The workspace does not include Node.js or npm
- The repository recommends VS Code extensions, but does not define a ready-made MSSQL connection profile
- The backend may fall back to the in-memory repository when SQL bootstrap fails
- The two test layers validate different concerns and should not be treated as interchangeable

## License

This repository uses [LICENSE](LICENSE) with The Unlicense. The intent is to place as few copyright restrictions as possible on reuse, modification, and redistribution.