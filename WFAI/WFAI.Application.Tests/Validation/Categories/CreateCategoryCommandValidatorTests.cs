using WFAI.Application.Features.Categories.Commands.Create;

namespace WFAI.Application.Tests.Validation.Categories;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new CreateCategoryCommand("Category", "category", null, true, 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_name_is_missing()
    {
        var command = new CreateCategoryCommand(string.Empty, "category", null, true, 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Fact]
    public void Validate_should_fail_when_parent_id_is_not_positive()
    {
        var command = new CreateCategoryCommand("Category", "category", 0, true, 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCategoryCommand.ParentId));
    }
}