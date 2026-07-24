namespace WFAI.Application.Features.Token.Queries
{
    public class GetTokenQueryValidator : AbstractValidator<GetTokenQuery>
    {
        public GetTokenQueryValidator()
        {
            RuleFor(u => u.TokenRequest.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(u => u.TokenRequest.Password)
                .NotEmpty()
                .MinimumLength(6);
        }
    }
}