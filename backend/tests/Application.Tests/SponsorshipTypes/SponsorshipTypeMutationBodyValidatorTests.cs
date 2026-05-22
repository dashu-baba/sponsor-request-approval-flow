using FluentAssertions;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Application.SponsorshipTypes.Validators;

namespace SponsorshipApproval.Application.Tests.SponsorshipTypes;

public sealed class SponsorshipTypeMutationBodyValidatorTests
{
    private readonly SponsorshipTypeMutationBodyValidator _validator = new();

    [Fact]
    public async Task Valid_body_should_pass_validation()
    {
        var result = await _validator
            .ValidateAsync(CreateValidBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Missing_name_should_fail_validation()
    {
        var result = await _validator
            .ValidateAsync(CreateValidBody() with { Name = "" }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(SponsorshipTypeMutationBody.Name));
    }

    [Fact]
    public async Task Name_longer_than_120_characters_should_fail_validation()
    {
        var result = await _validator
            .ValidateAsync(CreateValidBody() with { Name = new string('A', 121) }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(SponsorshipTypeMutationBody.Name));
    }

    [Fact]
    public async Task Description_longer_than_1000_characters_should_fail_validation()
    {
        var result = await _validator
            .ValidateAsync(
                CreateValidBody() with { Description = new string('A', 1001) },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(SponsorshipTypeMutationBody.Description));
    }

    private static SponsorshipTypeMutationBody CreateValidBody() =>
        new(
            Name: "Community Grant",
            Description: "Community grant sponsorships.");
}
