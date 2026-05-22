using FluentValidation;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Application.Auth.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(AuthConstants.DisplayNameMaxLength);

        RuleFor(request => request.Department)
            .MaximumLength(AuthConstants.DepartmentMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Department));
    }
}
