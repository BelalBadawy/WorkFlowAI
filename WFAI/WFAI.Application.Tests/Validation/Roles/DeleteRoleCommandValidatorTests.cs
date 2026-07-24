using WFAI.Application.Features.Roles.Commands;

namespace WFAI.Application.Tests.Validation.Roles;

public class DeleteRoleCommandValidatorTests
{
    private readonly DeleteRoleCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_role_ids(int roleId)
    {
        var result = _validator.Validate(new DeleteRoleCommand { RoleId = roleId });

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .Should()
            .Contain("Role ID must be greater than 0.");
    }

    [Fact]
    public void Validate_should_pass_for_positive_role_id()
    {
        var result = _validator.Validate(new DeleteRoleCommand { RoleId = 7 });

        result.IsValid.Should().BeTrue();
    }
}