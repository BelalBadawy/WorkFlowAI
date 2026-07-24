namespace WFAI.Application.Features.Roles.Queries
{
    public class GetPermissionsQueryValidator : AbstractValidator<GetPermissionsQuery>
    {
        public GetPermissionsQueryValidator()
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("Role ID must be greater than 0.");
        }
    }
}