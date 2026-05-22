using FluentValidation;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;

namespace SponsorshipApproval.Application.SponsorshipTypes.Validators;

public sealed class DeleteSponsorshipTypeCommandValidator : AbstractValidator<DeleteSponsorshipTypeCommand>
{
    public DeleteSponsorshipTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
