using WFAI.Application.Features.Roles.Queries;

namespace WFAI.Application.Tests.Validation.Roles;

public class GetPermissionsQueryValidatorTests
{
    private readonly GetPermissionsQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_role_ids(int roleId)
    {
        var result = _validator.Validate(new GetPermissionsQuery { RoleId = roleId });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetPermissionsQuery.RoleId));
    }

    [Fact]
    public void Validate_should_pass_for_positive_role_id()
    {
        var result = _validator.Validate(new GetPermissionsQuery { RoleId = 7 });

        result.IsValid.Should().BeTrue();
    }
}