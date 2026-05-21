using FluentValidation;
using SponsorshipApproval.Application.Requests.Commands;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class UpdateDraftRequestCommandValidator : AbstractValidator<UpdateDraftRequestCommand>
{
    public UpdateDraftRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Body).SetValidator(new RequestMutationBodyValidator());
    }
}
