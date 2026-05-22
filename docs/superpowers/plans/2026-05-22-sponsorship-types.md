# Sponsorship Types Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add SystemAdmin-only CRUD endpoints for the sponsorship-type lookup.

**Architecture:** Keep endpoints thin and dispatch MediatR commands/queries. Application owns request/response records and validators; Infrastructure owns EF Core handlers and enforces uniqueness plus soft-disable delete semantics.

**Tech Stack:** ASP.NET Core Minimal APIs, MediatR, FluentValidation, EF Core, xUnit, FluentAssertions, Testcontainers-backed integration tests.

---

### Task 1: Contracts And Validator Tests

**Files:**
- Create: `backend/src/Application/SponsorshipTypes/Models/SponsorshipTypeDtos.cs`
- Create: `backend/src/Application/SponsorshipTypes/Commands/CreateSponsorshipTypeCommand.cs`
- Create: `backend/src/Application/SponsorshipTypes/Commands/UpdateSponsorshipTypeCommand.cs`
- Create: `backend/src/Application/SponsorshipTypes/Commands/DeleteSponsorshipTypeCommand.cs`
- Create: `backend/src/Application/SponsorshipTypes/Queries/ListSponsorshipTypesQuery.cs`
- Create: `backend/src/Application/SponsorshipTypes/Validators/SponsorshipTypeMutationBodyValidator.cs`
- Create: `backend/tests/Application.Tests/SponsorshipTypes/SponsorshipTypeMutationBodyValidatorTests.cs`

- [x] Write validator tests for valid input, missing name, overlong name, and overlong description.
- [x] Run the validator tests and confirm they fail because the new types do not exist.
- [x] Add DTOs, commands, queries, and validator.
- [x] Re-run validator tests and confirm they pass.

### Task 2: Handler Behavior

**Files:**
- Create: `backend/src/Infrastructure/SponsorshipTypes/Handlers/ListSponsorshipTypesQueryHandler.cs`
- Create: `backend/src/Infrastructure/SponsorshipTypes/Handlers/CreateSponsorshipTypeCommandHandler.cs`
- Create: `backend/src/Infrastructure/SponsorshipTypes/Handlers/UpdateSponsorshipTypeCommandHandler.cs`
- Create: `backend/src/Infrastructure/SponsorshipTypes/Handlers/DeleteSponsorshipTypeCommandHandler.cs`
- Create: `backend/src/Infrastructure/SponsorshipTypes/SponsorshipTypeProjection.cs`
- Create: `backend/tests/Api.IntegrationTests/SponsorshipTypes/SponsorshipTypeAdminTests.cs`

- [x] Write integration tests for admin CRUD, non-admin forbidden, duplicate active name conflict, and soft-delete of a referenced type.
- [x] Run the integration tests and confirm they fail because endpoints are missing.
- [x] Implement handlers with case-insensitive active-name uniqueness and `DELETE` as `IsActive = false`.
- [x] Re-run the integration tests and confirm the targeted tests pass.

### Task 3: API And Documentation

**Files:**
- Create: `backend/src/Api/Endpoints/SponsorshipTypeEndpoints.cs`
- Modify: `backend/src/Api/Program.cs`
- Modify: `docs/tasks/T2.3-sponsorship-types.md`

- [x] Map `GET/POST/PUT/DELETE /sponsorship-types` behind `AuthorizationPolicies.SystemAdmin`.
- [x] Document that delete soft-disables records to preserve referenced requests.
- [ ] Run backend build, format verification, and all backend tests.
- [ ] Commit with a conventional commit message and no co-author trailer.
- [ ] Push the task branch and create a PR with verification evidence.
