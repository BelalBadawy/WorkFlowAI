using WFAI.Application.Features.Users.Queries;

namespace WFAI.Application.Tests.Validation.Users;

public class GetUserByIdQueryValidatorTests
{
    private readonly GetUserByIdQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_ids(int userId)
    {
        var result = _validator.Validate(new GetUserByIdQuery { UserId = userId });

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .Should()
            .Contain("UserId must be greater than 0.");
    }

    [Fact]
    public void Validate_should_pass_for_positive_ids()
    {
        var result = _validator.Validate(new GetUserByIdQuery { UserId = 99 });

        result.IsValid.Should().BeTrue();
    }
}