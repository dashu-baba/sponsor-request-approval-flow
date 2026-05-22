using FluentAssertions;
using FluentValidation.TestHelper;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Validators;

namespace SponsorshipApproval.Application.Tests.Requests;

public sealed class TransitionRequestCommandValidatorTests
{
    [Fact]
    public void Reject_without_remarks_should_fail()
    {
        var validator = new RejectRequestCommandValidator();
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Reject_with_whitespace_remarks_should_fail()
    {
        var validator = new RejectRequestCommandValidator();
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: "   ");
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Reject_with_valid_remarks_should_pass()
    {
        var validator = new RejectRequestCommandValidator();
        var command = new RejectRequestCommand(Guid.NewGuid(), Remarks: "Budget exceeded.");
        var result = validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(c => c.Remarks);
    }

    [Fact]
    public void Reject_with_empty_id_should_fail()
    {
        var validator = new RejectRequestCommandValidator();
        var command = new RejectRequestCommand(Guid.Empty, Remarks: "Some reason.");
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Approve_without_remarks_should_pass()
    {
        var validator = new ApproveRequestCommandValidator();
        var command = new ApproveRequestCommand(Guid.NewGuid(), Remarks: null);
        var result = validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Approve_with_empty_id_should_fail()
    {
        var validator = new ApproveRequestCommandValidator();
        var command = new ApproveRequestCommand(Guid.Empty, Remarks: null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Submit_command_with_valid_id_should_pass()
    {
        var validator = new SubmitRequestCommandValidator();
        var command = new SubmitRequestCommand(Guid.NewGuid());
        var result = validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Submit_command_with_empty_id_should_fail()
    {
        var validator = new SubmitRequestCommandValidator();
        var command = new SubmitRequestCommand(Guid.Empty);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Cancel_command_with_valid_id_should_pass()
    {
        var validator = new CancelRequestCommandValidator();
        var command = new CancelRequestCommand(Guid.NewGuid(), Remarks: null);
        var result = validator.TestValidate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Cancel_command_with_empty_id_should_fail()
    {
        var validator = new CancelRequestCommandValidator();
        var command = new CancelRequestCommand(Guid.Empty, Remarks: null);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
