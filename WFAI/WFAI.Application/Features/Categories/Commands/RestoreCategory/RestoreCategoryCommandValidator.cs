using FluentValidation;

namespace WFAI.Application.Features.Categories.Commands.RestoreCategory
{
    public class RestoreCategoryCommandValidator : AbstractValidator<RestoreCategoryCommand>
    {
        public RestoreCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");
        }
    }
}