using FluentAssertions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Validators;

namespace SponsorshipApproval.Application.Tests.Requests;

public sealed class RequestMutationBodyValidatorTests
{
    private readonly RequestMutationBodyValidator _validator = new();

    [Fact]
    public async Task Valid_body_should_pass_validation()
    {
        var result = await _validator.ValidateAsync(CreateValidBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Requested_amount_must_be_greater_than_zero(decimal amount)
    {
        var result = await _validator
            .ValidateAsync(CreateValidBody() with { RequestedAmount = amount }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RequestMutationBody.RequestedAmount));
    }

    [Fact]
    public async Task Event_date_in_the_past_should_fail_validation()
    {
        var result = await _validator.ValidateAsync(
                CreateValidBody() with { EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RequestMutationBody.EventDate));
    }

    [Fact]
    public async Task Missing_sponsorship_type_should_fail_validation()
    {
        var result = await _validator
            .ValidateAsync(
                CreateValidBody() with { SponsorshipTypeId = Guid.Empty },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RequestMutationBody.SponsorshipTypeId));
    }

    [Fact]
    public async Task Missing_title_should_fail_validation()
    {
        var result = await _validator
            .ValidateAsync(CreateValidBody() with { Title = "" }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RequestMutationBody.Title));
    }

    private static RequestMutationBody CreateValidBody() =>
        new(
            Title: "Community booth sponsorship",
            Department: "Engineering",
            SponsorshipTypeId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            EventName: "Tech Community Day",
            EventDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount: 1500m,
            Purpose: "Support a local developer outreach event.",
            ExpectedBenefit: "Brand visibility with engineering talent.",
            Remarks: "Draft");
}
