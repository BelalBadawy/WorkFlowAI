namespace WFAI.Application.Features.Users.Queries
{
    public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(u => u.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.");
        }
    }
}