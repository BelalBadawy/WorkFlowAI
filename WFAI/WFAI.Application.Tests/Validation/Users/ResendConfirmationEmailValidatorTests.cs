using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class ResendConfirmationEmailValidatorTests
{
    private readonly ResendConfirmationEmailValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ResendConfirmationEmailCommand
        {
            ResendConfirmation = TestData.ResendConfirmationEmailRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_email_is_empty()
    {
        var command = new ResendConfirmationEmailCommand
        {
            ResendConfirmation = TestData.ResendConfirmationEmailRequest(email: "")
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResendConfirmation.Email");
    }

    [Fact]
    public void Validate_should_fail_when_email_is_invalid_format()
    {
        var command = new ResendConfirmationEmailCommand
        {
            ResendConfirmation = TestData.ResendConfirmationEmailRequest(email: "not-an-email")
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResendConfirmation.Email");
    }
}