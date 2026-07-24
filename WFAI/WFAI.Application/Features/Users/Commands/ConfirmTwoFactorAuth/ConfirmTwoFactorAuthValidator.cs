namespace WFAI.Application.Features.Users.Commands.ConfirmTwoFactorAuth
{
    public class ConfirmTwoFactorAuthValidator
        : AbstractValidator<ConfirmTwoFactorAuthCommand>
    {
        public ConfirmTwoFactorAuthValidator()
        {
            RuleFor(x => x.Request.Code)
                .NotEmpty()
                .Matches(@"^\d{6}$")
                .WithMessage("Code must be exactly 6 digits.");
        }
    }
}