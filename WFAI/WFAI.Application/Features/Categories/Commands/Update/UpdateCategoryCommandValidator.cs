
namespace WFAI.Application.Features.Categories.Commands.Update
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Valid Category Id is required.");

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

            RuleFor(x => x)
                .Must(x => !x.ParentId.HasValue || x.ParentId.Value != x.Id)
                .WithMessage("A category cannot be its own parent.");

            RuleFor(x => x.RowVersion)
                .NotNull()
                .Must(rowVersion => rowVersion is { Length: > 0 })
                .WithMessage("RowVersion is required for concurrency checks.");
        }
    }
}