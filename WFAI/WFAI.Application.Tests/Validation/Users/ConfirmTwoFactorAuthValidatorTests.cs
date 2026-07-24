using WFAI.Application.Features.Users.Commands.ConfirmTwoFactorAuth;
using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Tests.Validation.Users;

public class ConfirmTwoFactorAuthValidatorTests
{
    private readonly ConfirmTwoFactorAuthValidator _validator = new();

    [Fact]
    public void Validate_EmptyCode_ReturnsValidationFailure()
    {
        var command = new ConfirmTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NonNumericCode_ReturnsValidationFailure()
    {
        var command = new ConfirmTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "abc123" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Code must be exactly 6 digits.");
    }

    [Fact]
    public void Validate_CodeShorterThanSixDigits_ReturnsValidationFailure()
    {
        var command = new ConfirmTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "12345" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Code must be exactly 6 digits.");
    }

    [Fact]
    public void Validate_SixDigitNumericCode_PassesValidation()
    {
        var command = new ConfirmTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "123456" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}