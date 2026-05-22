using FluentAssertions;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Auth.Validators;

namespace SponsorshipApproval.Application.Tests.Auth;

public sealed class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_should_pass()
    {
        var result = await _validator
            .ValidateAsync(new UpdateProfileRequest("Jane Doe", "Marketing"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_display_name_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new UpdateProfileRequest("", "Marketing"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Whitespace_only_display_name_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new UpdateProfileRequest("   ", "Marketing"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Display_name_over_max_length_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new UpdateProfileRequest(new string('A', 121), null), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Department_over_max_length_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new UpdateProfileRequest("Jane Doe", new string('B', 121)), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }
}

public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_should_pass()
    {
        var result = await _validator
            .ValidateAsync(new ChangePasswordRequest("Password1!", "Password2!"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Missing_current_password_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new ChangePasswordRequest("", "Password2!"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Short_new_password_should_fail()
    {
        var result = await _validator
            .ValidateAsync(new ChangePasswordRequest("Password1!", "short"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
    }
}
