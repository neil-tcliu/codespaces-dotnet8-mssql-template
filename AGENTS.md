# Project Guidelines

## Architecture

This project uses Feature Folders.

- Organize application code by feature or domain first.
- Keep files that change together in the same feature folder.
- Avoid top-level folders that split the codebase only by technical layer when the project grows.

Preferred structure:

```text
src/
  features/
    auth/
      api/
      components/
      hooks/
      types/
      utils/
    mssql/
      api/
      components/
      hooks/
      types/
      utils/
```

Avoid structures like this as the primary organization model:

```text
src/
  components/
  hooks/
  types/
  utils/
```

## Conventions

- Put new code under the feature that owns the behavior.
- Share code only when at least two features truly depend on the same abstraction.
- If shared code is needed, place it in a clearly named shared area such as `src/shared/`.
- Keep feature-local tests, types, helpers, and UI close to the feature.
- Prefer small public surfaces between features over cross-feature imports into internals.

## Tech Stack

- Use .NET 8 as the project baseline for apps, libraries, and tests unless a change explicitly requires a newer target framework.
- Use Dapper for SQL data access in the MSSQL feature unless there is a clear existing abstraction that must be preserved.
- Keep package and framework versions aligned with the rest of the repository when adding new projects.

## Build and Test

- Build the console sample with `dotnet build src/features/mssql/MssqlConsoleSample.csproj`.
- Run the console sample with `dotnet run --project src/features/mssql/MssqlConsoleSample.csproj`.
- Build the integration tests with `dotnet build src/features/mssql/tests/MssqlConsoleSample.Tests.csproj`.
- Run the integration tests with `dotnet test src/features/mssql/tests/MssqlConsoleSample.Tests.csproj`.
- Build the web backend with `dotnet build src/features/mssql/backend/MssqlCrudBackend.csproj`.
- Run the web backend with `dotnet run --project src/features/mssql/backend/MssqlCrudBackend.csproj`.
- Run the web backend API tests with `dotnet test src/features/mssql/backend/tests/MssqlCrudBackend.Tests.csproj`.
- If a port is already in use by a process started during the current task, stop that process and reuse the original port instead of switching to a different port.
- Keep future build and test commands documented here as new apps or services are added.

## Code Style

- Keep changes aligned with the existing file and folder naming already used in the project.
- Prefer minimal, direct implementations over framework-heavy scaffolding unless the project already depends on it.