namespace WFAI.Application.Features.Users.Commands
{
    public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {
        public UpdateUserRolesCommandValidator()
        {
            RuleFor(x => x.UpdateUserRoles.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.UpdateUserRoles.Roles)
                .NotNull().WithMessage("Roles list cannot be null.")
                .Must(r => r.Count > 0).WithMessage("At least one role must be assigned.");

            RuleForEach(x => x.UpdateUserRoles.Roles).ChildRules(role =>
            {
                role.RuleFor(r => r)
                    .NotEmpty()
                    .WithMessage("RoleName is required.");

                role.RuleFor(r => r)
                    .MaximumLength(256)
                    .WithMessage("RoleName cannot exceed 256 characters.");
            });

        }
    }
}