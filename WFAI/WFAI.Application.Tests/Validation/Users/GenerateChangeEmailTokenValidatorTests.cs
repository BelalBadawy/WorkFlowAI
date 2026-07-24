using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class GenerateChangeEmailTokenValidatorTests
{
    private readonly GenerateChangeEmailTokenValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new GenerateChangeEmailTokenCommand
        {
            GenerateChangeEmailToken = TestData.GenerateChangeEmailTokenRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_new_email_is_empty()
    {
        var command = new GenerateChangeEmailTokenCommand
        {
            GenerateChangeEmailToken = TestData.GenerateChangeEmailTokenRequest(newEmail: "")
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GenerateChangeEmailToken.NewEmail");
    }

    [Fact]
    public void Validate_should_fail_when_new_email_is_invalid_format()
    {
        var command = new GenerateChangeEmailTokenCommand
        {
            GenerateChangeEmailToken = TestData.GenerateChangeEmailTokenRequest(newEmail: "not-an-email")
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GenerateChangeEmailToken.NewEmail");
    }
}