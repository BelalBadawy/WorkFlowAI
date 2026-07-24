using WFAI.Application.Features.Categories.Commands.Delete;

namespace WFAI.Application.Tests.Validation.Categories;

public class DeleteCategoryCommandValidatorTests
{
    private readonly DeleteCategoryCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_for_non_positive_category_ids(int categoryId)
    {
        var result = _validator.Validate(new DeleteCategoryCommand(categoryId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(DeleteCategoryCommand.Id));
    }

    [Fact]
    public void Validate_should_pass_for_positive_category_id()
    {
        var result = _validator.Validate(new DeleteCategoryCommand(7));

        result.IsValid.Should().BeTrue();
    }
}