# Backend — Sponsorship Request Approval Workflow

ASP.NET Core 10 API following Clean Architecture + CQRS-lite (MediatR).

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0.x (`dotnet --version`) |

## Project structure

```
backend/
├── src/
│   ├── Domain/           # Entities, enums, workflow state machine, domain rules. No external deps.
│   ├── Application/      # Commands/queries + handlers, DTOs, validators, port interfaces.
│   ├── Infrastructure/   # EF Core, repositories, JWT, MinIO adapter. (wired in later tasks)
│   └── Api/              # Thin endpoints, DI wiring, middleware, OpenAPI.
├── tests/
│   ├── Domain.Tests/          # State-machine + RBAC unit tests.
│   ├── Application.Tests/     # Handler tests with fakes.
│   └── Api.IntegrationTests/  # WebApplicationFactory + Testcontainers end-to-end tests.
├── Directory.Build.props       # Repo-wide MSBuild defaults (net10.0, Nullable, analyzers).
├── Directory.Packages.props    # Central Package Management — all NuGet versions here.
└── .editorconfig               # Formatting + analyzer severity rules (enforced source of truth).
```

## Build

```bash
cd backend
dotnet restore
dotnet build
```

The build enforces `TreatWarningsAsErrors=true`. Zero warnings is the bar.

## Run locally

```bash
dotnet run --project src/Api
```

The API starts on `https://localhost:7000` (or the port in `launchSettings.json`).

## Test

```bash
dotnet test
```

Integration tests (`Api.IntegrationTests`) will require Docker for Testcontainers — added in a later task.

## Format check

```bash
dotnet format --verify-no-changes
```

This is the same check CI runs. Fix any issues with `dotnet format` (without `--verify-no-changes`).
