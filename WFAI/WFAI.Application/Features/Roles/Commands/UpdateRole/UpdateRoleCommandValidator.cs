namespace WFAI.Application.Features.Roles.Commands
{
    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator()
        {

            RuleFor(x => x.UpdateRole.RoleId)
                .NotEqual(0).WithMessage("RoleId is required.");

            RuleFor(x => x.UpdateRole.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.UpdateRole.Description)
                .MaximumLength(256).WithMessage("Description must not exceed 256 characters.");
        }
    }
}