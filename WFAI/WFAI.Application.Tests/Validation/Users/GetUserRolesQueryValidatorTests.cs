using WFAI.Application.Features.Users.Queries;

namespace WFAI.Application.Tests.Validation.Users;

public class GetUserRolesQueryValidatorTests
{
    private readonly GetUserRolesQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_user_ids(int userId)
    {
        var result = _validator.Validate(new GetUserRolesQuery { UserId = userId });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetUserRolesQuery.UserId));
    }

    [Fact]
    public void Validate_should_pass_for_positive_user_id()
    {
        var result = _validator.Validate(new GetUserRolesQuery { UserId = 7 });

        result.IsValid.Should().BeTrue();
    }
}