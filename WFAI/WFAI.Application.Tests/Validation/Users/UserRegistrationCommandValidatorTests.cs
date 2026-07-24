using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class UserRegistrationCommandValidatorTests
{
    private readonly UserRegistrationCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UserRegistrationCommand
        {
            UserRegistration = TestData.UserRegistrationRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_password_confirmation_does_not_match()
    {
        var request = TestData.UserRegistrationRequest();
        request.ConfirmPassword = "Different@123";

        var command = new UserRegistrationCommand { UserRegistration = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .Should()
            .Contain("Passwords do not match.");
    }

    [Fact]
    public void Validate_should_fail_when_email_is_invalid()
    {
        var request = TestData.UserRegistrationRequest();
        request.Email = "not-an-email";

        var command = new UserRegistrationCommand { UserRegistration = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .Should()
            .Contain("Invalid email format.");
    }
}