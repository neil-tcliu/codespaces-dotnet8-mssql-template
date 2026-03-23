# Install Steps

This document explains how to install and validate the repository in a local machine or in GitHub Codespaces. The goal is not only to make the projects compile, but to make the full development environment understandable: container roles, dependencies, persistence paths, test modes, and common misunderstandings. This workspace combines a Dev Container, a Docker Compose SQL Server sidecar, .NET 8 projects, and VS Code workspace settings. If you treat them as the same layer, troubleshooting becomes unnecessarily difficult.

## Installation Goals

By the end of this setup, you should have and understand the following:

- A workspace container for editing code and running `dotnet` commands
- A separate `sqlserver` container running SQL Server 2022
- .NET 8 SDK and runtime installed through the Dev Container feature
- A runnable console sample
- A runnable ASP.NET Core backend
- A set of console integration tests that hit real SQL Server
- A set of backend API tests that use ASP.NET Core TestServer and an in-memory repository

## Understand the Three Layers First

Before starting the installation, separate these three concepts clearly.

### 1. Project

The project is the source code, configuration, and documentation stored in the repository, for example:

- [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json)
- [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml)
- [src/features/mssql/MssqlConsoleSample.csproj](src/features/mssql/MssqlConsoleSample.csproj)
- [src/features/mssql/backend/MssqlCrudBackend.csproj](src/features/mssql/backend/MssqlCrudBackend.csproj)

The project defines what you are building, how it runs, and which containers and external services it expects. The project itself is not a container.

### 2. Image

An image is the base template used to create a container. This workspace uses at least two images:

- `mcr.microsoft.com/devcontainers/base:ubuntu-24.04` for the workspace container
- `mcr.microsoft.com/mssql/server:2022-latest` for the SQL Server container

The image is the source template, not the live environment you interact with.

### 3. Container

A container is a running instance created from an image. The terminal where you run commands is the workspace container. The service that actually provides the database is the `sqlserver` container. They are different containers connected through the same Docker Compose network.

## Prerequisites

If you are working locally, your machine needs:

- Visual Studio Code
- The Dev Containers extension
- Docker Desktop, Docker Engine, or another compatible container runtime

If you are working in GitHub Codespaces, the container runtime is typically provided by the platform, but the project still depends on these files:

- [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json)
- [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml)

The workspace also includes VS Code configuration files. Current status:

- [.vscode/extensions.json](.vscode/extensions.json) recommends the C# and MSSQL extensions
- [.vscode/settings.json](.vscode/settings.json) is currently empty and does not define a built-in MSSQL connection profile

That means the MSSQL extension can be recommended automatically, but you still need to create your own connection profile if you want to browse the database from VS Code.

## The First Setting Most Users Should Change

If you are handing this repository to another user, the first thing they usually need to change is the SQL Server SA password. The intended place to change it is:

- `.devcontainer/.env`

Start from [.devcontainer/.env.example](.devcontainer/.env.example) and create your own `.devcontainer/.env`, then update this value:

```text
MSSQL_SA_PASSWORD=MSSQL_SA_PASSWORD_CHANGE_ME
```

This is intentionally a named placeholder, not a usable password. Replace it with your own strong password immediately after creating `.devcontainer/.env`. That file is not tracked by Git. Docker Compose will pass the same value to both the `workspace` and `sqlserver` services, and the .NET projects will read the same environment variable at startup to override the password portion of their connection strings. This avoids editing several `appsettings.json` files by hand.

## Dev Container and SQL Server Layout

### Workspace Container

The workspace container is defined as the `workspace` service in [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml) and selected as the active service by [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json). Its role is to:

- Mount the full repository at `/workspaces/codespaces-blank`
- Provide the Ubuntu 24.04 development environment
- Install .NET 8 through the Dev Container feature
- Host your `dotnet build`, `dotnet run`, and `dotnet test` commands

### SQL Server Container

The SQL Server container is defined as the `sqlserver` service in [.devcontainer/docker-compose.yml](.devcontainer/docker-compose.yml). Its role is to:

- Run SQL Server 2022
- Expose the hostname `sqlserver` to the workspace container
- Mount data, log, and secrets folders into persistent directories inside the project

The volume mapping is:

- `../src/features/mssql/.sqlserver/data` to `/var/opt/mssql/data`
- `../src/features/mssql/.sqlserver/log` to `/var/opt/mssql/log`
- `../src/features/mssql/.sqlserver/secrets` to `/var/opt/mssql/secrets`

So the SQL Server engine runs inside a container, but the persisted files live under [src/features/mssql/.sqlserver](src/features/mssql/.sqlserver) in the repository tree rather than only inside the container filesystem.

## Recommended Installation Sequence

### 1. Open the Repository Root

Open the repository root in VS Code, the directory containing:

- [README.md](README.md)
- [INSTALL.md](INSTALL.md)
- [codespaces-blank.sln](codespaces-blank.sln)

### 2. Rebuild and Reopen in the Dev Container

Run this VS Code command:

```text
Dev Containers: Rebuild and Reopen in Container
```

This is the central step in the whole process because it moves the workspace into the container topology that the repository actually expects, instead of trying to run the project from the host or from an outdated container state.

After this step, you should expect at least the following:

- The workspace container starts on Ubuntu 24.04
- The .NET 8 feature is installed in the workspace container
- The `sqlserver` sidecar container is started by Docker Compose
- Both containers are attached to the same workspace network

If you just changed `MSSQL_SA_PASSWORD` in `.devcontainer/.env`, this rebuild step is also what applies the new value to the containers.

### 3. Verify .NET 8 SDK and Runtime

Inside the workspace container, run:

```bash
dotnet --list-sdks
dotnet --list-runtimes
```

You should see .NET 8 entries. This matters because:

- `dotnet build` only proves the compile step works
- `dotnet run` and `dotnet test` also require the correct runtime

If you only have the SDK but not the runtime, you can end up in a state where builds succeed but execution and tests fail.

### 4. Verify That `sqlserver` Resolves

Inside the workspace container, run:

```bash
getent hosts sqlserver
```

If this returns a result, the SQL Server sidecar is visible on the current workspace network. This is more reliable than guessing `localhost`, because the project connection strings do not target `localhost`. They target `sqlserver`.

### 5. Understand the Connection Strings and Database File Paths

This repository currently has two main application settings files:

- [src/features/mssql/appsettings.json](src/features/mssql/appsettings.json)
- [src/features/mssql/backend/appsettings.json](src/features/mssql/backend/appsettings.json)

From those settings:

- The console sample connects by default to `DapperSampleDb` on `sqlserver:1433`
- The backend connects by default to `MssqlCrudBackendDb` on `sqlserver:1433`
- The backend also defines these database file paths:
  - `/var/opt/mssql/data/MssqlCrudBackendDb.mdf`
  - `/var/opt/mssql/log/MssqlCrudBackendDb_log.ldf`

Those are paths as seen from inside the SQL Server container. Their persistent location in the project is determined by the Docker Compose volume mapping.

These tracked settings files now use the named placeholder `MSSQL_SA_PASSWORD_CHANGE_ME`, not a usable default password. If the `MSSQL_SA_PASSWORD` environment variable exists, the application overrides the password portion of the tracked connection strings with that value. If you do not set it, you should expect SQL Server startup or application connection attempts to fail until you replace the placeholder with your own strong password.

### 6. Install VS Code Extensions If Needed

The repository already provides extension recommendations. If you want graphical SQL Server access in VS Code, install at least:

- `ms-dotnettools.csharp`
- `ms-mssql.mssql`

Their roles are:

- `ms-dotnettools.csharp` for .NET development support
- `ms-mssql.mssql` for SQL Server connections and query tooling

If you only want to run the sample and the tests, this is not a strict requirement. If you want to inspect the database from VS Code, it is useful.

### 7. Build the Console Sample

Run:

```bash
dotnet build src/features/mssql/MssqlConsoleSample.csproj
```

This project demonstrates:

- Connecting to SQL Server through Dapper
- Initializing the database
- Creating the `dbo.Authors` table
- Reading and writing author records

### 8. Run the Console Sample

Run:

```bash
dotnet run --project src/features/mssql/MssqlConsoleSample.csproj
```

On the first successful run, you should expect it to:

- Create `DapperSampleDb`
- Create `dbo.Authors`
- Insert sample data
- Read the data back and print it to the console

### 9. Run the Console Integration Tests

Run:

```bash
dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj
```

These are real SQL Server integration tests, not mocks and not unit tests. They validate CRUD behavior including:

- Select
- Insert
- Update
- Delete

Their success depends on all of the following being true:

- The .NET 8 runtime is available
- The `sqlserver` container resolves and accepts connections
- SQL Server is reachable on the current workspace network

### 10. Build and Run the Web Backend

Build:

```bash
dotnet build src/features/mssql/backend/MssqlCrudBackend.csproj
```

Run:

```bash
dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj
```

The backend startup path has one important behavior:

- It tries to connect to SQL Server and initialize `MssqlCrudBackendDb`
- If bootstrap fails and `AllowInMemoryFallbackOnBootstrapFailure` is `true`, it falls back to the in-memory repository

That means the backend can run in two modes:

- `sqlserver`
- `in-memory`

You can see the current mode from the `/api/health` response.

### 11. Run the Backend API Tests

Run:

```bash
dotnet test src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj
```

These tests are different from the console integration tests. They do not connect to real SQL Server. Instead they:

- Use ASP.NET Core `WebApplicationFactory`
- Force the environment to `Testing`
- Replace the SQL Server repository with an in-memory repository

That means the backend tests verify API behavior and request/response flow, not real SQL Server I/O.

## Common Misunderstandings and Troubleshooting Priorities

### Misunderstanding 1: This project includes Node.js or npm

It does not. The workspace container declares the .NET 8 feature in [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json), but not a Node.js feature. That is why `dotnet` is available while `npm` is not.

### Misunderstanding 2: SQL Server is installed directly on the Linux host

It is not. SQL Server runs in a container started from `mcr.microsoft.com/mssql/server:2022-latest`. The host only needs a compatible container runtime. You do not need to install SQL Server directly into the Ubuntu host environment.

### Misunderstanding 3: SQL Server data files are not part of the project layout

This needs a precise distinction:

- The SQL Server process runs inside a container
- The data and log folders are mounted into [src/features/mssql/.sqlserver](src/features/mssql/.sqlserver)

So it is correct to say that the persistence directories live inside the project, but incorrect to say that the database engine itself runs inside the project.

### Misunderstanding 4: A successful build means the environment is fully working

It does not. A successful build only means compilation succeeded. This repository also depends on:

- The runtime
- The SQL Server sidecar
- The correct Docker Compose network
- The correct connection string configuration

That is why `build`, `run`, and `test` should be validated separately.

### Misunderstanding 5: The SQL data directory is `.data/mssql`

It is not. The current layout uses [src/features/mssql/.sqlserver](src/features/mssql/.sqlserver) with `data`, `log`, and `secrets` subdirectories. If any document still says `.data/mssql`, treat that as outdated documentation rather than the actual project state.

### Misunderstanding 6: The workspace already includes a ready-to-use MSSQL extension profile

It does not. [.vscode/settings.json](.vscode/settings.json) is currently empty. Even though [.vscode/extensions.json](.vscode/extensions.json) recommends the extension, you still need to create your own MSSQL connection profile in VS Code if you want GUI access.

## Shortest Successful Path

If you want the fastest path to validate the environment end to end, use this order:

1. Open the repository root
2. Run `Dev Containers: Rebuild and Reopen in Container`
3. Run `dotnet --list-runtimes` and confirm .NET 8 is present
4. Run `getent hosts sqlserver` and confirm the sidecar resolves
5. Run `dotnet run --project src/features/mssql/MssqlConsoleSample.csproj`
6. Run `dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj`
7. Run `dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj`
8. Check `/api/health` and confirm which repository mode the backend is using

## Post-Installation Validation Criteria

Do not treat installation as successful because one command worked. At minimum, all of the following should be true:

- `dotnet build src/features/mssql/MssqlConsoleSample.csproj` succeeds
- `dotnet run --project src/features/mssql/MssqlConsoleSample.csproj` succeeds
- `dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj` succeeds
- `dotnet build src/features/mssql/backend/MssqlCrudBackend.csproj` succeeds
- `dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj` starts successfully
- `/api/health` responds and shows whether the backend is using `sqlserver` or `in-memory`
- `dotnet test src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj` succeeds

If any step fails, do not change application code first. Check these things first:

- Whether the containers were actually rebuilt
- Whether `sqlserver` resolves from the workspace container
- Whether the .NET 8 runtime is really installed
- Whether `localhost` was used by mistake instead of `sqlserver`
- Whether you assumed the backend API tests connect to real SQL Server