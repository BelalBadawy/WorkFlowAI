using WFAI.Application.Features.Token.Queries.LoginWith2FA;

namespace WFAI.Application.Tests.Validation.Token;

public class LoginWith2FAQueryValidatorTests
{
    private readonly LoginWith2FAQueryValidator _validator = new();

    [Fact]
    public void Validate_EmptyChallengeToken_ReturnsValidationFailure()
    {
        var command = new LoginWith2FAQuery
        {
            Request = new TwoFactorLoginRequest { TwoFactorChallengeToken = "", Code = "123456" }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("TwoFactorChallengeToken"));
    }

    [Fact]
    public void Validate_EmptyCode_ReturnsValidationFailure()
    {
        var command = new LoginWith2FAQuery
        {
            Request = new TwoFactorLoginRequest { TwoFactorChallengeToken = "challenge-token", Code = "" }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Code"));
    }

    [Fact]
    public void Validate_BothFieldsPopulated_PassesValidation()
    {
        var command = new LoginWith2FAQuery
        {
            Request = new TwoFactorLoginRequest
            {
                TwoFactorChallengeToken = "valid-challenge-token",
                Code = "123456"
            }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}