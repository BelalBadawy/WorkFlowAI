using WFAI.Application.Features.Categories.Commands.ChangeCategoryStatus;
using FluentAssertions;

namespace WFAI.Application.Tests.Validation.Categories;

public class ChangeCategoryStatusCommandValidatorTests
{
    private readonly ChangeCategoryStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ChangeCategoryStatusCommand(1, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_id_is_zero_or_negative()
    {
        var command = new ChangeCategoryStatusCommand(0, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangeCategoryStatusCommand.Id));
    }
}