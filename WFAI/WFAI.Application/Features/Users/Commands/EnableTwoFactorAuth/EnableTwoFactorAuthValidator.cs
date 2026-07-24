namespace WFAI.Application.Features.Users.Commands.EnableTwoFactorAuth
{
    public class EnableTwoFactorAuthValidator
        : AbstractValidator<EnableTwoFactorAuthCommand>
    {
        public EnableTwoFactorAuthValidator()
        {
            RuleFor(x => x.Request.Code)
                .NotEmpty()
                .Matches(@"^\d{6}$")
                .WithMessage("Code must be exactly 6 digits.");
        }
    }
}