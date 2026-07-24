using WFAI.Application.Features.Categories.Queries.GetCategoryById;

namespace WFAI.Application.Tests.Validation.Categories;

public class GetCategoryByIdQueryValidatorTests
{
    private readonly GetCategoryByIdQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_category_ids(int categoryId)
    {
        var result = _validator.Validate(new GetCategoryByIdQuery(categoryId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetCategoryByIdQuery.Id));
    }

    [Fact]
    public void Validate_should_pass_for_positive_category_id()
    {
        var result = _validator.Validate(new GetCategoryByIdQuery(7));

        result.IsValid.Should().BeTrue();
    }
}