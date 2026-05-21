using FluentValidation;
using SponsorshipApproval.Application.Requests.Commands;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class CreateRequestCommandValidator : AbstractValidator<CreateRequestCommand>
{
    public CreateRequestCommandValidator()
    {
        RuleFor(command => command.Body).SetValidator(new RequestMutationBodyValidator());
    }
}
