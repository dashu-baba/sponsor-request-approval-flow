using FluentAssertions;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Auth.Validators;

namespace SponsorshipApproval.Application.Tests.Auth;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_should_pass()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest(
                    "new.user@test.local",
                    "New User",
                    "Engineering",
                    Roles.Requestor,
                    "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Missing_email_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("", "New User", null, Roles.Requestor, "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_email_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("not-an-email", "New User", null, Roles.Requestor, "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Empty_display_name_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("new.user@test.local", "", null, Roles.Requestor, "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Display_name_over_max_length_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest(
                    "new.user@test.local",
                    new string('A', 121),
                    null,
                    Roles.Requestor,
                    "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Department_over_max_length_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest(
                    "new.user@test.local",
                    "New User",
                    new string('B', 121),
                    Roles.Requestor,
                    "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnknownRole")]
    public async Task Invalid_role_should_fail(string role)
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("new.user@test.local", "New User", null, role, "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(Roles.Requestor)]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.FinanceAdmin)]
    [InlineData(Roles.SystemAdmin)]
    public async Task Supported_roles_should_pass(string role)
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("new.user@test.local", "New User", null, role, "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Short_initial_password_should_fail()
    {
        var result = await _validator
            .ValidateAsync(
                new CreateUserRequest("new.user@test.local", "New User", null, Roles.Requestor, "short"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }
}
