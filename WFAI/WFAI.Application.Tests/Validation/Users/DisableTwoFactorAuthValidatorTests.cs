using WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth;

namespace WFAI.Application.Tests.Validation.Users;

public class DisableTwoFactorAuthValidatorTests
{
    private readonly DisableTwoFactorAuthValidator _validator = new();

    [Fact]
    public void Validate_EmptyPassword_ReturnsValidationFailure()
    {
        var command = new DisableTwoFactorAuthCommand
        {
            Request = new DisableTwoFactorAuthRequest { Password = "", Code = null }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Password"));
    }

    [Fact]
    public void Validate_PasswordPresentAndCodeNull_PassesValidation()
    {
        var command = new DisableTwoFactorAuthCommand
        {
            Request = new DisableTwoFactorAuthRequest { Password = "Pass@123", Code = null }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordPresentAndSixDigitCode_PassesValidation()
    {
        var command = new DisableTwoFactorAuthCommand
        {
            Request = new DisableTwoFactorAuthRequest { Password = "Pass@123", Code = "123456" }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordPresentAndNonNumericCode_ReturnsValidationFailure()
    {
        var command = new DisableTwoFactorAuthCommand
        {
            Request = new DisableTwoFactorAuthRequest { Password = "Pass@123", Code = "abcdef" }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Code must be exactly 6 digits.");
    }
}