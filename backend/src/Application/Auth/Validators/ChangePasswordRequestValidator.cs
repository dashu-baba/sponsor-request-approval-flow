using FluentValidation;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Application.Auth.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
