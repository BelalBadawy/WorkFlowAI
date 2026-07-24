using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class UpdateUserRolesCommandValidatorTests
{
    private readonly UpdateUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UpdateUserRolesCommand
        {
            UpdateUserRoles = TestData.UpdateUserRolesRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_roles_are_empty()
    {
        var request = TestData.UpdateUserRolesRequest();
        request.Roles = [];
        var command = new UpdateUserRolesCommand { UpdateUserRoles = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UpdateUserRoles.Roles");
    }

    [Fact]
    public void Validate_should_fail_when_a_role_name_is_empty()
    {
        var request = TestData.UpdateUserRolesRequest();
        request.Roles = ["Admin", string.Empty];
        var command = new UpdateUserRolesCommand { UpdateUserRoles = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName.StartsWith("UpdateUserRoles.Roles["));
    }
}