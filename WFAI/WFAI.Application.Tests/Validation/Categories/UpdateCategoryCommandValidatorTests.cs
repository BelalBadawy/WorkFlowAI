using WFAI.Application.Features.Categories.Commands.Update;

namespace WFAI.Application.Tests.Validation.Categories;

public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new UpdateCategoryCommand(1, "Category", "category", null, true, 1, [1]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_category_is_its_own_parent()
    {
        var command = new UpdateCategoryCommand(5, "Category", "category", 5, true, 1, [1]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "A category cannot be its own parent.");
    }

    [Fact]
    public void Validate_should_fail_when_row_version_is_missing()
    {
        var command = new UpdateCategoryCommand(1, "Category", "category", null, true, 1, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCategoryCommand.RowVersion));
    }
}