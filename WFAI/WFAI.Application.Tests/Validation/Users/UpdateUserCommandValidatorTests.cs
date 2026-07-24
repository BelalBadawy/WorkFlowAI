using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UpdateUserCommand
        {
            UpdateUser = TestData.UpdateUserRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_full_name_is_missing()
    {
        var request = TestData.UpdateUserRequest();
        request.FullName = string.Empty;
        var command = new UpdateUserCommand { UpdateUser = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UpdateUser.FullName");
    }

    [Fact]
    public void Validate_should_fail_when_phone_number_has_invalid_characters()
    {
        var request = TestData.UpdateUserRequest();
        request.PhoneNumber = "01012ABC678";
        var command = new UpdateUserCommand { UpdateUser = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UpdateUser.PhoneNumber");
    }
}