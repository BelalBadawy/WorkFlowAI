namespace WFAI.Application.Features.Users.Queries
{
    public class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
    {
        public GetUserRolesQueryValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be greater than 0.");
        }
    }
}