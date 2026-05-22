using FluentValidation;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Application.Auth.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.DisplayName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Display name is required.")
            .Must(name => name.Trim().Length <= AuthConstants.DisplayNameMaxLength)
            .WithMessage($"Display name must be {AuthConstants.DisplayNameMaxLength} characters or fewer.");

        RuleFor(request => request.Department)
            .MaximumLength(AuthConstants.DepartmentMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Department));

        RuleFor(request => request.Role)
            .NotEmpty()
            .Must(role => Roles.All.Contains(role))
            .WithMessage("Role must be one of the supported application roles.");

        RuleFor(request => request.InitialPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
