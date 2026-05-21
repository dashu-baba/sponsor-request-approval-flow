# Domain EF Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the dependency-free domain model, EF Core PostgreSQL persistence layer, initial migration, migrator wiring, and migration round-trip test for T1.1.

**Architecture:** Domain owns plain entities and enums only. Infrastructure owns `AppDbContext`, EF configurations, migrations, and DI registration. API composes Infrastructure at the boundary and Docker Compose runs migrations through a dedicated migrator stage.

**Tech Stack:** .NET 10, EF Core 10, Npgsql EF Core provider, EFCore.NamingConventions, PostgreSQL 17, xUnit v3, FluentAssertions, Testcontainers PostgreSQL.

---

### Task 1: Branch, Packages, and Failing Persistence Tests

**Files:**
- Modify: `backend/Directory.Packages.props`
- Modify: `backend/src/Infrastructure/SponsorshipApproval.Infrastructure.csproj`
- Modify: `backend/src/Api/SponsorshipApproval.Api.csproj`
- Modify: `backend/tests/Api.IntegrationTests/SponsorshipApproval.Api.IntegrationTests.csproj`
- Create: `backend/tests/Api.IntegrationTests/Persistence/AppDbContextTests.cs`

- [ ] Confirm branch is `feat/T1.1-domain-efcore`.
- [ ] Add latest stable EF Core/Npgsql/NamingConventions/Testcontainers package versions through CPM.
- [ ] Add a failing integration test that creates a PostgreSQL container, applies migrations, inserts a `SponsorshipRequest`, reads it back, and inspects EF metadata for `xmin`, `numeric(18,2)`, and `timestamp with time zone`.
- [ ] Run the focused test and confirm it fails because persistence types do not exist yet.

### Task 2: Dependency-Free Domain Model

**Files:**
- Create: `backend/src/Domain/Requests/RequestStatus.cs`
- Create: `backend/src/Domain/Requests/SponsorshipRequest.cs`
- Create: `backend/src/Domain/Requests/SponsorshipType.cs`
- Create: `backend/src/Domain/Requests/WorkflowHistory.cs`
- Create: `backend/src/Domain/Requests/Attachment.cs`
- Create: `backend/tests/Domain.Tests/Requests/DomainModelTests.cs`

- [ ] Add failing domain tests that assert default request status is `Draft` and Domain has no EF assembly references.
- [ ] Add plain C# entities with GUID primary keys, request fields from HLD §5, audit columns, `RequestorId` seam, and navigation collections.
- [ ] Run domain tests and confirm they pass.

### Task 3: EF Core Context, Configuration, and DI

**Files:**
- Create: `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/SponsorshipRequestConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/SponsorshipTypeConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/WorkflowHistoryConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/AttachmentConfiguration.cs`
- Create: `backend/src/Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/Api/Program.cs`
- Modify: `backend/src/Api/SponsorshipApproval.Api.csproj`

- [ ] Implement `AppDbContext` with `DbSet` properties and `ApplyConfigurationsFromAssembly`.
- [ ] Configure snake_case naming via Npgsql options in DI.
- [ ] Configure column types, required fields, max lengths, FKs, delete behavior, query-path indexes, and `xmin` concurrency token.
- [ ] Register `AddInfrastructure(builder.Configuration, builder.Environment)` from API startup.
- [ ] Run the focused integration test and confirm it now fails only because no migration exists.

### Task 4: Initial Migration and Docker Migrator

**Files:**
- Create: `backend/src/Infrastructure/Migrations/*_InitialDomainSchema.cs`
- Create: `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `backend/src/Api/Dockerfile`
- Modify: `docker-compose.yml`

- [ ] Run `dotnet ef migrations add InitialDomainSchema --project src/Infrastructure --startup-project src/Api --context AppDbContext`.
- [ ] Review generated migration for snake_case tables, `numeric(18,2)`, `timestamp with time zone`, FKs, indexes, and no `xmin` column creation.
- [ ] Replace `migrator-placeholder` with a `migrator` stage that installs/uses `dotnet-ef` and runs `dotnet ef database update`.
- [ ] Point Compose `migrator` at the real Dockerfile target.

### Task 5: Verification and Cleanup

**Files:**
- Modify only files needed to address verification failures.

- [ ] Run `dotnet build backend/SponsorshipApproval.sln`.
- [ ] Run `dotnet format backend/SponsorshipApproval.sln --verify-no-changes`.
- [ ] Run `dotnet test backend/SponsorshipApproval.sln`.
- [ ] If Docker is available, run a fresh migrator check with Compose.
- [ ] Re-read `docs/tasks/T1.1-domain-efcore.md` and verify every acceptance criterion is covered.
