using FluentValidation;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Application.Auth.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Display name is required.")
            .Must(name => name.Trim().Length <= AuthConstants.DisplayNameMaxLength)
            .WithMessage($"Display name must be {AuthConstants.DisplayNameMaxLength} characters or fewer.");

        RuleFor(request => request.Department)
            .MaximumLength(AuthConstants.DepartmentMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Department));
    }
}
