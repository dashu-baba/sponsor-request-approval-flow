using FluentValidation;
using SponsorshipApproval.Application.Requests.Commands;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class SubmitRequestCommandValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}

public sealed class CancelRequestCommandValidator : AbstractValidator<CancelRequestCommand>
{
    public CancelRequestCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}

public sealed class ApproveRequestCommandValidator : AbstractValidator<ApproveRequestCommand>
{
    public ApproveRequestCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}

public sealed class RejectRequestCommandValidator : AbstractValidator<RejectRequestCommand>
{
    public RejectRequestCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Remarks)
            .NotEmpty()
            .WithMessage("Remarks are required when rejecting a request.");
    }
}
