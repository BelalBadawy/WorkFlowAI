using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Roles;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new CreateRoleCommand
        {
            CreateRole = TestData.CreateRoleRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_name_is_missing()
    {
        var request = TestData.CreateRoleRequest();
        request.Name = string.Empty;
        var command = new CreateRoleCommand { CreateRole = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CreateRole.Name");
    }
}