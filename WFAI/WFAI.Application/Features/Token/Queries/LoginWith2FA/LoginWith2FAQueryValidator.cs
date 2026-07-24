namespace WFAI.Application.Features.Token.Queries.LoginWith2FA
{
    public class LoginWith2FAQueryValidator : AbstractValidator<LoginWith2FAQuery>
    {
        public LoginWith2FAQueryValidator()
        {
            RuleFor(x => x.Request.TwoFactorChallengeToken).NotEmpty();
            RuleFor(x => x.Request.Code).NotEmpty();
        }
    }
}