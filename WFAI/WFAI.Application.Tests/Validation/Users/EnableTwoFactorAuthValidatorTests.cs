using WFAI.Application.Features.Users.Commands.EnableTwoFactorAuth;
using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Tests.Validation.Users;

public class EnableTwoFactorAuthValidatorTests
{
    private readonly EnableTwoFactorAuthValidator _validator = new();

    [Fact]
    public void Validate_EmptyCode_ReturnsValidationFailure()
    {
        var command = new EnableTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NonNumericCode_ReturnsValidationFailure()
    {
        var command = new EnableTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "abcdef" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Code must be exactly 6 digits.");
    }

    [Fact]
    public void Validate_CodeShorterThanSixDigits_ReturnsValidationFailure()
    {
        var command = new EnableTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "12345" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Code must be exactly 6 digits.");
    }

    [Fact]
    public void Validate_SixDigitNumericCode_PassesValidation()
    {
        var command = new EnableTwoFactorAuthCommand { Request = new TwoFactorCodeRequest { Code = "654321" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}