using FluentValidation;

namespace WFAI.Application.Features.Phases.Commands.Create
{
    public class CreatePhaseCommandValidator : AbstractValidator<CreatePhaseCommand>
    {
        public CreatePhaseCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(150)
                .WithMessage("Title cannot exceed 150 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be greater than or equal to 0.");
        }
    }
}
