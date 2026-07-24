namespace WFAI.Application.Features.Roles.Commands
{
    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.CreateRole.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.CreateRole.Description)
                .MaximumLength(256).WithMessage("Description must not exceed 256 characters.");
        }
    }
}