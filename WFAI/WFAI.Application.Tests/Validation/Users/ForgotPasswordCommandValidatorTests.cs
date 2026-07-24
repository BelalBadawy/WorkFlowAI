using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Validation.Users;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ForgotPasswordCommand { Email = "user@example.com" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_email_is_invalid()
    {
        var command = new ForgotPasswordCommand { Email = "not-an-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ForgotPasswordCommand.Email));
    }
}