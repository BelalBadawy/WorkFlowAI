namespace WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth
{
    public class DisableTwoFactorAuthValidator
        : AbstractValidator<DisableTwoFactorAuthCommand>
    {
        public DisableTwoFactorAuthValidator()
        {
            RuleFor(x => x.Request.Password).NotEmpty();

            When(x => !string.IsNullOrEmpty(x.Request.Code), () =>
            {
                RuleFor(x => x.Request.Code)
                    .Matches(@"^\d{6}$")
                    .WithMessage("Code must be exactly 6 digits.");
            });
        }
    }
}