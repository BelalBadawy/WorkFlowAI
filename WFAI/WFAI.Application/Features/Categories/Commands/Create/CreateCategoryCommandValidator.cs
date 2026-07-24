
namespace WFAI.Application.Features.Categories.Commands.Create
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Slug)
                .NotEmpty()
                .WithMessage("Slug is required.")
                .MaximumLength(150)
                .WithMessage("Slug cannot exceed 150 characters.");

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be greater than or equal to 0.");

            RuleFor(x => x.ParentId)
                .GreaterThan(0)
                .When(x => x.ParentId.HasValue)
                .WithMessage("Parent category id must be greater than 0.");
        }
    }
}