namespace WFAI.Application.Features.Roles.Commands
{
    public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
    {
        public UpdateRolePermissionsCommandValidator()
        {

            RuleFor(x => x.UpdateRoleClaims)
                .NotNull().WithMessage("UpdateRoleClaims is required.");

            RuleFor(x => x.UpdateRoleClaims.RoleId)
                .GreaterThan(0).WithMessage("RoleId must be greater than 0.");

            RuleFor(x => x.UpdateRoleClaims.RoleClaims)
                .NotNull().WithMessage("RoleClaims list is required.")
                .Must(list => list.Any()).WithMessage("At least one RoleClaim must be provided.");

            RuleForEach(x => x.UpdateRoleClaims.RoleClaims).ChildRules(claim =>
            {
                claim.RuleFor(c => c.ClaimType)
                    .NotEmpty().WithMessage("ClaimType is required.");

                claim.RuleFor(c => c.ClaimValue)
                    .NotEmpty().WithMessage("ClaimValue is required.");

                claim.RuleFor(c => c.Description)
                    .NotEmpty().WithMessage("Description is required.");

                // Optional: add validation for IsAssignedToRole if needed
            });
        }
    }
}