using WFAI.Application.Features.Categories.Queries.GetCategoryByIdAdmin;

namespace WFAI.Application.Tests.Validation.Categories;

public class GetCategoryByIdAdminQueryValidatorTests
{
    private readonly GetCategoryByIdAdminQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_category_ids(int categoryId)
    {
        var result = _validator.Validate(new GetCategoryByIdAdminQuery(categoryId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetCategoryByIdAdminQuery.Id));
    }

    [Fact]
    public void Validate_should_pass_for_positive_category_id()
    {
        var result = _validator.Validate(new GetCategoryByIdAdminQuery(7));

        result.IsValid.Should().BeTrue();
    }
}