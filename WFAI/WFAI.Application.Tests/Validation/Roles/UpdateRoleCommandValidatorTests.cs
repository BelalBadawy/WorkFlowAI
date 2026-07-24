using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Roles;

public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UpdateRoleCommand
        {
            UpdateRole = TestData.UpdateRoleRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_role_id_is_missing()
    {
        var request = TestData.UpdateRoleRequest(roleId: 0);
        var command = new UpdateRoleCommand { UpdateRole = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UpdateRole.RoleId");
    }
}