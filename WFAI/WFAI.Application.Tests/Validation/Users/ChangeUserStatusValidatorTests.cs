using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class ChangeUserStatusValidatorTests
{
    private readonly ChangeUserStatusValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ChangeUserStatusCommand
        {
            ChangeUserStatus = TestData.ChangeUserStatusRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_user_id_is_missing()
    {
        var command = new ChangeUserStatusCommand
        {
            ChangeUserStatus = TestData.ChangeUserStatusRequest(userId: 0)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ChangeUserStatus.UserId");
    }
}