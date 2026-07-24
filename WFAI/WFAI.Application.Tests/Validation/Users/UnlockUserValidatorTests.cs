using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class UnlockUserValidatorTests
{
    private readonly UnlockUserValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UnlockUserCommand
        {
            UnlockUser = TestData.UnlockUserRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_user_id_is_zero()
    {
        var command = new UnlockUserCommand
        {
            UnlockUser = TestData.UnlockUserRequest(userId: 0)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnlockUser.UserId");
    }
}