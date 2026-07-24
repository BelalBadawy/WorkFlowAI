using WFAI.Application.Features.Users.Commands.Logout;

namespace WFAI.Application.Tests.Validation.Users;

public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyRefreshToken_ReturnsValidationFailure()
    {
        var command = new LogoutCommand { Request = new LogoutRequest { RefreshToken = "" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RefreshToken"));
    }

    [Fact]
    public void Validate_NonEmptyRefreshToken_PassesValidation()
    {
        var command = new LogoutCommand { Request = new LogoutRequest { RefreshToken = "valid-refresh-token" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}