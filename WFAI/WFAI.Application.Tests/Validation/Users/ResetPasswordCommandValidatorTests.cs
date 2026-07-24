using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ResetPasswordCommand
        {
            ResetPasswordRequest = TestData.ResetPasswordRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_token_is_missing()
    {
        var request = TestData.ResetPasswordRequest();
        request.Token = string.Empty;
        var command = new ResetPasswordCommand { ResetPasswordRequest = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ResetPasswordRequest.Token");
    }

    [Fact]
    public void Validate_should_fail_when_password_confirmation_does_not_match()
    {
        var request = TestData.ResetPasswordRequest();
        request.ConfirmPassword = "Different@123";
        var command = new ResetPasswordCommand { ResetPasswordRequest = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ResetPasswordRequest.ConfirmPassword");
    }
}