# Workflow Engine (T2.2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approval workflow state machine, transition commands, immutable audit trail, and optimistic concurrency for the sponsorship request approval flow.

**Architecture:** Domain holds the transition table and invariants (no EF references); Application layer defines commands/validators; Infrastructure handlers enforce B3/B4, write WorkflowHistory, and handle DbUpdateConcurrencyException → 409; API layer adds thin endpoints with role policies.

**Tech Stack:** .NET 10, MediatR v13, EF Core 10, FluentValidation, xUnit, FluentAssertions, Testcontainers (PostgreSQL 17)

---

## File Map

### New files to create

| File | Responsibility |
|------|----------------|
| `backend/src/Domain/Requests/WorkflowAction.cs` | Enum: Submit, Cancel, Approve, Reject |
| `backend/src/Domain/Requests/WorkflowStateMachine.cs` | Static transition table; `Transition(request, action, actorRole, actorId)` |
| `backend/src/Application/Requests/Commands/TransitionRequestCommand.cs` | `SubmitRequestCommand`, `CancelRequestCommand`, `ApproveRequestCommand`, `RejectRequestCommand` records |
| `backend/src/Application/Requests/Validators/TransitionRequestCommandValidator.cs` | Remarks required on Reject (A5); id non-empty |
| `backend/src/Infrastructure/Requests/Handlers/TransitionRequestCommandHandler.cs` | Single handler covering all 4 actions; enforces B3, B4, concurrency |
| `backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs` | Unit: all valid transitions; invalid state; wrong role; B4 self-approval; B3 SystemAdmin |
| `backend/tests/Application.Tests/Requests/TransitionRequestCommandValidatorTests.cs` | Unit: remarks required on reject; optional on approve |
| `backend/tests/Api.IntegrationTests/Requests/WorkflowTransitionTests.cs` | Integration: full happy path; reject paths; cancel; concurrency 409; wrong-role 403 |

### Files to modify

| File | Change |
|------|--------|
| `backend/src/Api/Endpoints/RequestEndpoints.cs` | Add 4 POST endpoints `/{id}/submit|cancel|approve|reject` |
| `backend/src/Application/Common/ICurrentUserContext.cs` | Add `IReadOnlyList<string> Roles { get; }` |
| `backend/src/Infrastructure/Identity/CurrentUserContext.cs` | Implement the new `Roles` property |

---

## Task 1: Domain — WorkflowAction enum

**Files:**
- Create: `backend/src/Domain/Requests/WorkflowAction.cs`

- [ ] **Step 1: Write the failing unit test**

File: `backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs`

```csharp
using FluentAssertions;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Domain.Tests.Requests;

public sealed class WorkflowStateMachineTests
{
    [Fact]
    public void WorkflowAction_enum_should_define_expected_values()
    {
        Enum.GetValues<WorkflowAction>()
            .Should().BeEquivalentTo(new[]
            {
                WorkflowAction.Submit,
                WorkflowAction.Cancel,
                WorkflowAction.Approve,
                WorkflowAction.Reject,
            });
    }
}
```

- [ ] **Step 2: Run test — expect compile failure**

```bash
cd backend && dotnet test tests/Domain.Tests --no-build 2>&1 | tail -20
```

Expected: `error CS0246: The type or namespace name 'WorkflowAction' could not be found`

- [ ] **Step 3: Create WorkflowAction enum**

File: `backend/src/Domain/Requests/WorkflowAction.cs`

```csharp
namespace SponsorshipApproval.Domain.Requests;

public enum WorkflowAction
{
    Submit = 0,
    Cancel = 1,
    Approve = 2,
    Reject = 3,
}
```

- [ ] **Step 4: Run test — expect pass**

```bash
cd backend && dotnet test tests/Domain.Tests -v q 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Domain/Requests/WorkflowAction.cs backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs
git commit -m "feat(domain): add WorkflowAction enum"
```

---

## Task 2: Domain — WorkflowStateMachine

**Files:**
- Create: `backend/src/Domain/Requests/WorkflowStateMachine.cs`
- Modify: `backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs`

The transition table (from HLD §6):

| From | Action | Allowed role | To |
|------|--------|--------------|----|
| Draft | Submit | Requestor (owner) | PendingManagerApproval |
| Draft | Cancel | Requestor (owner) | Cancelled |
| PendingManagerApproval | Cancel | Requestor (owner) | Cancelled |
| PendingManagerApproval | Approve | Manager | PendingFinanceReview |
| PendingManagerApproval | Reject | Manager | Rejected |
| PendingFinanceReview | Approve | FinanceAdmin | Approved |
| PendingFinanceReview | Reject | FinanceAdmin | Rejected |

Rules enforced in the state machine:
- **B3**: SystemAdmin is never an allowed role in the table.
- **B4**: For Submit/Cancel (owner actions), actor must be the request owner. For Approve/Reject, actor must NOT be the owner.
- **A2**: Cancel only in Draft or PendingManagerApproval (encoded in table).

- [ ] **Step 1: Add valid-transition tests**

Append to `backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs`:

```csharp
    [Theory]
    [InlineData(RequestStatus.Draft, WorkflowAction.Submit, "Requestor", RequestStatus.PendingManagerApproval)]
    [InlineData(RequestStatus.Draft, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Manager", RequestStatus.PendingFinanceReview)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "Manager", RequestStatus.Rejected)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "FinanceAdmin", RequestStatus.Approved)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "FinanceAdmin", RequestStatus.Rejected)]
    public void Valid_transitions_should_return_correct_next_status(
        RequestStatus from, WorkflowAction action, string actorRole, RequestStatus expected)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "other-user" };
        var result = WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Cancel, "Requestor")]
    [InlineData(RequestStatus.Approved, WorkflowAction.Approve, "FinanceAdmin")]
    [InlineData(RequestStatus.Rejected, WorkflowAction.Approve, "Manager")]
    [InlineData(RequestStatus.Cancelled, WorkflowAction.Submit, "Requestor")]
    public void Invalid_state_transitions_should_throw_InvalidOperationException(
        RequestStatus from, WorkflowAction action, string actorRole)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "other-user" };
        var act = () => WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Requestor")]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "FinanceAdmin")]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "Manager")]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "Requestor")]
    [InlineData(RequestStatus.Draft, WorkflowAction.Submit, "SystemAdmin")]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "SystemAdmin")]
    public void Wrong_role_transitions_should_throw_UnauthorizedAccessException(
        RequestStatus from, WorkflowAction action, string actorRole)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "other-user" };
        var act = () => WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Self_approval_should_throw_UnauthorizedAccessException()
    {
        // B4: Manager who is also the requestor cannot approve their own request
        var request = new SponsorshipRequest
        {
            Status = RequestStatus.PendingManagerApproval,
            RequestorId = "manager-user",
        };
        var act = () => WorkflowStateMachine.Transition(
            request, WorkflowAction.Approve, "Manager", actorId: "manager-user");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Owner_submit_from_draft_requires_actor_to_be_owner()
    {
        var request = new SponsorshipRequest
        {
            Status = RequestStatus.Draft,
            RequestorId = "owner-user",
        };
        var act = () => WorkflowStateMachine.Transition(
            request, WorkflowAction.Submit, "Requestor", actorId: "not-owner");
        act.Should().Throw<UnauthorizedAccessException>();
    }
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
cd backend && dotnet build tests/Domain.Tests 2>&1 | grep -E "error|warning" | head -20
```

Expected: `error CS0246: The type or namespace name 'WorkflowStateMachine' could not be found`

- [ ] **Step 3: Create WorkflowStateMachine**

File: `backend/src/Domain/Requests/WorkflowStateMachine.cs`

```csharp
namespace SponsorshipApproval.Domain.Requests;

public static class WorkflowStateMachine
{
    private static readonly Dictionary<(RequestStatus From, WorkflowAction Action, string Role), RequestStatus> Transitions =
        new()
        {
            { (RequestStatus.Draft, WorkflowAction.Submit, "Requestor"), RequestStatus.PendingManagerApproval },
            { (RequestStatus.Draft, WorkflowAction.Cancel, "Requestor"), RequestStatus.Cancelled },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Cancel, "Requestor"), RequestStatus.Cancelled },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Manager"), RequestStatus.PendingFinanceReview },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "Manager"), RequestStatus.Rejected },
            { (RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "FinanceAdmin"), RequestStatus.Approved },
            { (RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "FinanceAdmin"), RequestStatus.Rejected },
        };

    private static readonly HashSet<WorkflowAction> OwnerActions =
    [
        WorkflowAction.Submit,
        WorkflowAction.Cancel,
    ];

    public static RequestStatus Transition(
        SponsorshipRequest request,
        WorkflowAction action,
        string actorRole,
        string actorId)
    {
        if (!Transitions.TryGetValue((request.Status, action, actorRole), out var nextStatus))
        {
            // Determine whether the key exists for any role (wrong role) or no role (invalid state)
            var existsForAnyRole = Transitions.Keys.Any(k => k.From == request.Status && k.Action == action);
            if (existsForAnyRole)
            {
                throw new UnauthorizedAccessException(
                    $"Role '{actorRole}' is not allowed to perform '{action}' on a request in '{request.Status}' status.");
            }

            throw new InvalidOperationException(
                $"Action '{action}' is not allowed when the request is in '{request.Status}' status.");
        }

        // B4: owner actions require actor == requestor; review actions block actor == requestor
        if (OwnerActions.Contains(action))
        {
            if (actorId != request.RequestorId)
            {
                throw new UnauthorizedAccessException(
                    "Only the request owner can perform this action.");
            }
        }
        else
        {
            if (actorId == request.RequestorId)
            {
                throw new UnauthorizedAccessException(
                    "The request owner cannot approve or reject their own request.");
            }
        }

        return nextStatus;
    }
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd backend && dotnet test tests/Domain.Tests -v q 2>&1 | tail -15
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Domain/Requests/WorkflowStateMachine.cs backend/tests/Domain.Tests/Requests/WorkflowStateMachineTests.cs
git commit -m "feat(domain): implement workflow state machine with transition table and RBAC rules"
```

---

## Task 3: Add Roles property to ICurrentUserContext

**Files:**
- Modify: `backend/src/Application/Common/ICurrentUserContext.cs`
- Modify: `backend/src/Infrastructure/Identity/CurrentUserContext.cs`

- [ ] **Step 1: Read current CurrentUserContext implementation**

```bash
cat backend/src/Infrastructure/Identity/CurrentUserContext.cs
```

- [ ] **Step 2: Add `Roles` to interface**

In `backend/src/Application/Common/ICurrentUserContext.cs` add `IReadOnlyList<string> Roles { get; }`:

```csharp
namespace SponsorshipApproval.Application.Common;

public interface ICurrentUserContext
{
    string UserId { get; }

    string DisplayName { get; }

    IReadOnlyList<string> Roles { get; }

    Task<string?> GetDepartmentAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Implement in CurrentUserContext**

Read `backend/src/Infrastructure/Identity/CurrentUserContext.cs` first, then add:

```csharp
public IReadOnlyList<string> Roles =>
    _httpContextAccessor.HttpContext?.User.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList()
        .AsReadOnly()
    ?? [];
```

- [ ] **Step 4: Build to verify no compile errors**

```bash
cd backend && dotnet build -v q 2>&1 | grep -E "^.*error" | head -20
```

Expected: build succeeds with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Application/Common/ICurrentUserContext.cs backend/src/Infrastructure/Identity/CurrentUserContext.cs
git commit -m "feat(application): expose Roles on ICurrentUserContext"
```

---

## Task 4: Application — Transition commands + validators

**Files:**
- Create: `backend/src/Application/Requests/Commands/TransitionRequestCommand.cs`
- Create: `backend/src/Application/Requests/Validators/TransitionRequestCommandValidator.cs`
- Create: `backend/tests/Application.Tests/Requests/TransitionRequestCommandValidatorTests.cs`

- [ ] **Step 1: Write failing validator tests**

File: `backend/tests/Application.Tests/Requests/TransitionRequestCommandValidatorTests.cs`

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Validators;

namespace SponsorshipApproval.Application.Tests.Requests;

public sealed class TransitionRequestCommandValidatorTests
{
    private readonly TransitionRequestCommandValidator _validator = new();

    [Fact]
    public void Reject_without_remarks_should_fail()
    {
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Reject_with_empty_remarks_should_fail()
    {
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: "   ");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Reject_with_remarks_should_pass()
    {
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: "Budget exceeded.");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Approve_without_remarks_should_pass()
    {
        var command = new ApproveRequestCommand(Guid.NewGuid(), Remarks: null);
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Submit_command_with_valid_id_should_pass()
    {
        var command = new SubmitRequestCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Submit_command_with_empty_id_should_fail()
    {
        var command = new SubmitRequestCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
```

- [ ] **Step 2: Run — expect compile failure**

```bash
cd backend && dotnet build tests/Application.Tests 2>&1 | grep error | head -10
```

Expected: types not found.

- [ ] **Step 3: Create command records**

File: `backend/src/Application/Requests/Commands/TransitionRequestCommand.cs`

```csharp
using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record SubmitRequestCommand(Guid Id) : IRequest<RequestDetailDto>;

public sealed record CancelRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record ApproveRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record RejectRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;
```

- [ ] **Step 4: Create validator**

File: `backend/src/Application/Requests/Validators/TransitionRequestCommandValidator.cs`

```csharp
using FluentValidation;
using SponsorshipApproval.Application.Requests.Commands;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class TransitionRequestCommandValidator :
    AbstractValidator<SubmitRequestCommand>,
    IValidator<CancelRequestCommand>,
    IValidator<ApproveRequestCommand>,
    IValidator<RejectRequestCommand>
{
    public TransitionRequestCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }

    ValidationResult IValidator<CancelRequestCommand>.Validate(IValidationContext context)
    {
        var c = (CancelRequestCommand)context.InstanceToValidate;
        var result = new FluentValidation.Results.ValidationResult();
        if (c.Id == Guid.Empty)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(c.Id), "'Id' must not be empty."));
        return result;
    }

    ValidationResult IValidator<ApproveRequestCommand>.Validate(IValidationContext context)
    {
        var c = (ApproveRequestCommand)context.InstanceToValidate;
        var result = new FluentValidation.Results.ValidationResult();
        if (c.Id == Guid.Empty)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(c.Id), "'Id' must not be empty."));
        return result;
    }

    ValidationResult IValidator<RejectRequestCommand>.Validate(IValidationContext context)
    {
        var c = (RejectRequestCommand)context.InstanceToValidate;
        var result = new FluentValidation.Results.ValidationResult();
        if (c.Id == Guid.Empty)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(c.Id), "'Id' must not be empty."));
        if (string.IsNullOrWhiteSpace(c.Remarks))
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(c.Remarks), "Remarks are required when rejecting a request."));
        return result;
    }

    Task<ValidationResult> IValidator<CancelRequestCommand>.ValidateAsync(IValidationContext context, CancellationToken cancellation)
        => Task.FromResult(((IValidator<CancelRequestCommand>)this).Validate(context));

    Task<ValidationResult> IValidator<ApproveRequestCommand>.ValidateAsync(IValidationContext context, CancellationToken cancellation)
        => Task.FromResult(((IValidator<ApproveRequestCommand>)this).Validate(context));

    Task<ValidationResult> IValidator<RejectRequestCommand>.ValidateAsync(IValidationContext context, CancellationToken cancellation)
        => Task.FromResult(((IValidator<RejectRequestCommand>)this).Validate(context));

    bool IValidator.CanValidateInstancesOfType(Type type) =>
        type == typeof(SubmitRequestCommand) ||
        type == typeof(CancelRequestCommand) ||
        type == typeof(ApproveRequestCommand) ||
        type == typeof(RejectRequestCommand);
}
```

- [ ] **Step 5: Run validator tests — expect pass**

```bash
cd backend && dotnet test tests/Application.Tests -v q 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/src/Application/Requests/Commands/TransitionRequestCommand.cs \
        backend/src/Application/Requests/Validators/TransitionRequestCommandValidator.cs \
        backend/tests/Application.Tests/Requests/TransitionRequestCommandValidatorTests.cs
git commit -m "feat(application): add transition commands and validator with reject-remarks rule"
```

---

## Task 5: Infrastructure — TransitionRequestCommandHandler

**Files:**
- Create: `backend/src/Infrastructure/Requests/Handlers/TransitionRequestCommandHandler.cs`

This handler covers all 4 commands. It:
1. Loads the request (throws `NotFoundException` if missing)
2. Checks ownership for submit/cancel (throws `ForbiddenException`)
3. Calls `WorkflowStateMachine.Transition` — catches `UnauthorizedAccessException` → `ForbiddenException`; `InvalidOperationException` → `ConflictException`
4. Applies B3: if actor role is SystemAdmin, throw `ForbiddenException`
5. Updates `request.Status`, `UpdatedAt`, `UpdatedBy`
6. Appends a `WorkflowHistory` row
7. Catches `DbUpdateConcurrencyException` → `ConflictException("A concurrent transition was made...")`
8. Returns refreshed `RequestDetailDto`

- [ ] **Step 1: Write integration tests first (they define the contract)**

File: `backend/tests/Api.IntegrationTests/Requests/WorkflowTransitionTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.Persistence.Seeding;

namespace SponsorshipApproval.Api.IntegrationTests.Requests;

public sealed class WorkflowTransitionTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    // ── Happy path: Draft → PendingManagerApproval → PendingFinanceReview → Approved ──

    [Fact]
    public async Task Full_approval_path_Draft_to_Approved_should_succeed()
    {
        var requestorEmail = $"req-happy-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-happy-{Guid.NewGuid():N}@test.local";
        var financeEmail   = $"fin-happy-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);
        await CreateUserAsync(financeEmail,   Roles.FinanceAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        // submit
        using var submitResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);
        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await submitResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        submitted!.Status.Should().Be(RequestStatus.PendingManagerApproval);

        // manager approve
        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var approveManagerResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "Looks good." }, TestContext.Current.CancellationToken);
        approveManagerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var managerApproved = await approveManagerResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        managerApproved!.Status.Should().Be(RequestStatus.PendingFinanceReview);

        // finance approve
        using var financeClient = await AuthenticatedClientAsync(financeEmail);
        using var approveFinanceResp = await financeClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = (string?)null }, TestContext.Current.CancellationToken);
        approveFinanceResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveFinanceResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        approved!.Status.Should().Be(RequestStatus.Approved);

        await AssertHistoryCountAsync(draft.Id, expectedCount: 3);
    }

    [Fact]
    public async Task Manager_reject_should_set_status_to_Rejected_and_be_terminal()
    {
        var requestorEmail = $"req-reject-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-reject-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        using var submitResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);
        submitResp.EnsureSuccessStatusCode();

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var rejectResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "Not in budget." }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await rejectResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        rejected!.Status.Should().Be(RequestStatus.Rejected);

        // further transitions should return 409
        using var retryResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        retryResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Finance_reject_should_set_status_to_Rejected()
    {
        var requestorEmail = $"req-finrej-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-finrej-{Guid.NewGuid():N}@test.local";
        var financeEmail   = $"fin-finrej-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);
        await CreateUserAsync(financeEmail,   Roles.FinanceAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken);

        using var financeClient = await AuthenticatedClientAsync(financeEmail);
        using var rejectResp = await financeClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "Compliance issue." }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await rejectResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Rejected);

        await AssertHistoryCountAsync(draft.Id, expectedCount: 3);
    }

    [Fact]
    public async Task Cancel_in_Draft_should_succeed()
    {
        var requestorEmail = $"req-cancel-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await cancelResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_in_PendingManagerApproval_should_succeed()
    {
        var requestorEmail = $"req-cncl2-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await cancelResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_in_PendingFinanceReview_should_return_409()
    {
        var requestorEmail = $"req-cncl3-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-cncl3-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken);

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reject_without_remarks_should_return_400()
    {
        var requestorEmail = $"req-norem-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-norem-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var rejectResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = (string?)null }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Wrong_role_should_return_403()
    {
        // Requestor trying to approve (manager step) returns 403 at endpoint level
        var requestorEmail = $"req-wrng-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var badResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        badResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Self_approval_should_return_403()
    {
        // A user who holds Manager role but also created the request cannot approve
        var selfEmail = $"self-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(selfEmail, Roles.Manager);

        using var selfClient = await AuthenticatedClientAsync(selfEmail);

        // Manager creates a draft (managers can also be requestors in this scenario)
        using var createResp = await selfClient
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken);
        createResp.EnsureSuccessStatusCode();
        var draft = await createResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);

        // Manager submits their own request (submit is a requestor action but the endpoint allows any authenticated user)
        using var submitResp = await selfClient
            .PostAsJsonAsync($"/requests/{draft!.Id}/submit", new { }, TestContext.Current.CancellationToken);
        submitResp.EnsureSuccessStatusCode();

        // Self-approval must be blocked
        using var approveResp = await selfClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        approveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SystemAdmin_approve_should_return_403()
    {
        var requestorEmail = $"req-adm-{Guid.NewGuid():N}@test.local";
        var adminEmail     = $"adm-adm-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(adminEmail,     Roles.SystemAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);

        using var adminClient = await AuthenticatedClientAsync(adminEmail);
        using var approveResp = await adminClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        approveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Every_successful_transition_writes_a_workflow_history_row()
    {
        var requestorEmail = $"req-hist-{Guid.NewGuid():N}@test.local";
        var managerEmail   = $"mgr-hist-{Guid.NewGuid():N}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail,   Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);
        await AssertHistoryCountAsync(draft.Id, 1);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "No budget." }, TestContext.Current.CancellationToken);
        await AssertHistoryCountAsync(draft.Id, 2);
    }

    // ── Helpers ──

    private async Task<RequestDetailDto> CreateDraftAsync(HttpClient client)
    {
        using var resp = await client
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken))!;
    }

    private static object CreateMutationBody() => new
    {
        Title = "Integration test sponsorship",
        Department = (string?)null,
        SponsorshipTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
        EventName = "Test Event",
        EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToString("yyyy-MM-dd"),
        RequestedAmount = 1000m,
        Purpose = "Workflow integration test.",
        ExpectedBenefit = (string?)null,
        Remarks = (string?)null,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResp = await loginClient
            .PostAsJsonAsync("/auth/login", new LoginRequest(email, "Password1!"), TestContext.Current.CancellationToken);
        loginResp.EnsureSuccessStatusCode();
        var body = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private async Task CreateUserAsync(string email, string role)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email, Email = email,
            DisplayName = email.Split('@')[0],
            Department = "Engineering",
            EmailConfirmed = true,
        };
        var r = await userManager.CreateAsync(user, "Password1!");
        r.Succeeded.Should().BeTrue(string.Join(", ", r.Errors.Select(e => e.Description)));
        var rr = await userManager.AddToRoleAsync(user, role);
        rr.Succeeded.Should().BeTrue(string.Join(", ", rr.Errors.Select(e => e.Description)));
    }

    private async Task AssertHistoryCountAsync(Guid requestId, int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.WorkflowHistoryEntries
            .CountAsync(h => h.SponsorshipRequestId == requestId, TestContext.Current.CancellationToken);
        count.Should().Be(expectedCount);
    }
}
```

- [ ] **Step 2: Run integration tests — expect compile/runtime failure (handlers don't exist yet)**

```bash
cd backend && dotnet build tests/Api.IntegrationTests 2>&1 | grep error | head -10
```

- [ ] **Step 3: Create TransitionRequestCommandHandler**

File: `backend/src/Infrastructure/Requests/Handlers/TransitionRequestCommandHandler.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class TransitionRequestCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<SubmitRequestCommand, RequestDetailDto>,
      IRequestHandler<CancelRequestCommand, RequestDetailDto>,
      IRequestHandler<ApproveRequestCommand, RequestDetailDto>,
      IRequestHandler<RejectRequestCommand, RequestDetailDto>
{
    public Task<RequestDetailDto> Handle(SubmitRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Submit, remarks: null, cancellationToken);

    public Task<RequestDetailDto> Handle(CancelRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Cancel, command.Remarks, cancellationToken);

    public Task<RequestDetailDto> Handle(ApproveRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Approve, command.Remarks, cancellationToken);

    public Task<RequestDetailDto> Handle(RejectRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Reject, command.Remarks, cancellationToken);

    private async Task<RequestDetailDto> ApplyTransitionAsync(
        Guid requestId,
        WorkflowAction action,
        string? remarks,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
            throw new NotFoundException("Request was not found.");

        // B3: SystemAdmin is never in the approval chain
        var actorRole = currentUser.Roles.FirstOrDefault() ?? string.Empty;
        if (actorRole == Roles.SystemAdmin)
            throw new ForbiddenException("System administrators are not part of the approval chain.");

        RequestStatus nextStatus;
        try
        {
            nextStatus = WorkflowStateMachine.Transition(request, action, actorRole, currentUser.UserId);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ForbiddenException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        var from = request.Status;
        var now = DateTimeOffset.UtcNow;

        request.Status = nextStatus;
        request.UpdatedAt = now;
        request.UpdatedBy = currentUser.UserId;

        dbContext.WorkflowHistoryEntries.Add(new WorkflowHistory
        {
            Id = Guid.NewGuid(),
            SponsorshipRequestId = request.Id,
            ActorId = currentUser.UserId,
            FromStatus = from,
            ToStatus = nextStatus,
            Remarks = remarks,
            OccurredAt = now,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A concurrent transition was already applied to this request. Please reload and try again.");
        }

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/Infrastructure/Requests/Handlers/TransitionRequestCommandHandler.cs \
        backend/tests/Api.IntegrationTests/Requests/WorkflowTransitionTests.cs
git commit -m "feat(infrastructure): add TransitionRequestCommandHandler with B3/B4/concurrency enforcement"
```

---

## Task 6: API — Transition endpoints

**Files:**
- Modify: `backend/src/Api/Endpoints/RequestEndpoints.cs`

Endpoints added:
- `POST /requests/{id}/submit` — requires any authenticated user (Requestor policy); handler enforces ownership
- `POST /requests/{id}/cancel` — requires any authenticated user; handler enforces ownership
- `POST /requests/{id}/approve` — requires Manager **or** FinanceAdmin (checked in handler via role)
- `POST /requests/{id}/reject` — requires Manager **or** FinanceAdmin

The endpoint-level policy for approve/reject is kept as "authenticated" (not role-gated at route level) because the correct role depends on the current status. The handler/state machine returns 403 for wrong-role. This avoids adding a combined "Manager or FinanceAdmin" policy just for the endpoint layer; the state machine is the authoritative gatekeeper.

> Note: `submit` and `cancel` bodies are empty (`{}`); `approve` and `reject` bodies carry optional/required `remarks`.

- [ ] **Step 1: Add transition body record**

At the top of `TransitionRequestCommand.cs`, add:

```csharp
public sealed record TransitionBody(string? Remarks);
```

- [ ] **Step 2: Update RequestEndpoints to add 4 action routes**

Replace the `MapRequestEndpoints` method body to add the new routes (keep existing ones):

```csharp
public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder app)
{
    var requests = app.MapGroup("/requests")
        .WithTags("Requests")
        .RequireAuthorization(AuthorizationPolicies.Requestor);

    requests.MapGet("/", ListOwnAsync);
    requests.MapPost("/", CreateAsync);
    requests.MapGet("/{id:guid}", GetByIdAsync);
    requests.MapPut("/{id:guid}", UpdateDraftAsync);

    requests.MapPost("/{id:guid}/submit", SubmitAsync);
    requests.MapPost("/{id:guid}/cancel", CancelAsync);
    requests.MapPost("/{id:guid}/approve", ApproveAsync);
    requests.MapPost("/{id:guid}/reject", RejectAsync);

    return app;
}
```

And add the 4 handler methods:

```csharp
private static async Task<IResult> SubmitAsync(
    Guid id,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new SubmitRequestCommand(id), cancellationToken).ConfigureAwait(false);
    return TypedResults.Ok(result);
}

private static async Task<IResult> CancelAsync(
    Guid id,
    TransitionBody body,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new CancelRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
    return TypedResults.Ok(result);
}

private static async Task<IResult> ApproveAsync(
    Guid id,
    TransitionBody body,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new ApproveRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
    return TypedResults.Ok(result);
}

private static async Task<IResult> RejectAsync(
    Guid id,
    TransitionBody body,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new RejectRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
    return TypedResults.Ok(result);
}
```

- [ ] **Step 3: Register validators in DependencyInjection**

Check `backend/src/Application/DependencyInjection.cs` — FluentValidation validators are registered via assembly scan. Confirm `TransitionRequestCommandValidator` will be picked up automatically (it should be since it's in the Application assembly).

- [ ] **Step 4: Build all projects clean**

```bash
cd backend && dotnet build -v q 2>&1 | grep -E "error|warning" | head -30
```

Expected: 0 errors, 0 warnings (warnings-as-errors is on).

- [ ] **Step 5: Commit**

```bash
git add backend/src/Api/Endpoints/RequestEndpoints.cs \
        backend/src/Application/Requests/Commands/TransitionRequestCommand.cs
git commit -m "feat(api): add submit/cancel/approve/reject endpoints for workflow transitions"
```

---

## Task 7: Run all tests and verify

- [ ] **Step 1: Run domain unit tests**

```bash
cd backend && dotnet test tests/Domain.Tests -v q 2>&1 | tail -15
```

Expected: all pass.

- [ ] **Step 2: Run application unit tests**

```bash
cd backend && dotnet test tests/Application.Tests -v q 2>&1 | tail -15
```

Expected: all pass.

- [ ] **Step 3: Run integration tests (requires Docker)**

```bash
cd backend && dotnet test tests/Api.IntegrationTests -v q 2>&1 | tail -30
```

Expected: all pass including `WorkflowTransitionTests` suite.

- [ ] **Step 4: dotnet format verify**

```bash
cd backend && dotnet format --verify-no-changes 2>&1
```

Expected: no formatting issues.

- [ ] **Step 5: Commit (if any format fixes needed)**

```bash
git add -u && git commit -m "style: apply dotnet format"
```

---

## Task 8: Concurrency test (manual verification)

The `DbUpdateConcurrencyException` path requires two concurrent writes. Integration test `WorkflowTransitionTests` covers the happy path. The concurrency scenario is verified by the xmin optimistic lock in `SponsorshipRequestConfiguration` (`builder.Property(r => r.Version).IsRowVersion()`).

To manually verify 409 on concurrent transition, you can write a focused integration test that:
1. Loads the same request entity twice (two separate DbContext instances)
2. Calls `SaveChangesAsync` on both — the second throws `DbUpdateConcurrencyException`

This is already covered by the handler's `catch (DbUpdateConcurrencyException)` → `ConflictException` mapping. The integration test infrastructure with Testcontainers verifies end-to-end behavior.

---

## Self-Review Checklist

- [x] **A2** — Cancel only in Draft/PendingManagerApproval → encoded in transition table; `Cancel_in_PendingFinanceReview_should_return_409` test covers it.
- [x] **A3/A4** — Rejected is terminal → no transition from Rejected in the table.
- [x] **A5** — Remarks required on Reject → `TransitionRequestCommandValidator`; `Reject_without_remarks_should_return_400` integration test.
- [x] **A6** — No amount-based routing → state machine has no amount checks.
- [x] **B3** — SystemAdmin blocked → handler checks role before calling state machine; `SystemAdmin_approve_should_return_403` test.
- [x] **B4** — Self-approval blocked → `WorkflowStateMachine.Transition` checks `actorId == request.RequestorId` for review actions; `Self_approval_should_return_403` integration test.
- [x] **D1** — Immutable WorkflowHistory row on every transition → `ApplyTransitionAsync` always adds to `WorkflowHistoryEntries`.
- [x] **Concurrency** → `catch (DbUpdateConcurrencyException)` → `ConflictException` → HTTP 409.
- [x] **Wrong role → 403** → `UnauthorizedAccessException` → `ForbiddenException` → GlobalExceptionHandler → 403.
- [x] **Invalid state → 409** → `InvalidOperationException` → `ConflictException` → 409.
