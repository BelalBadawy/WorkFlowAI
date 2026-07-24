namespace WFAI.Application.Features.Users.Commands
{
    public class GenerateChangeEmailTokenValidator : AbstractValidator<GenerateChangeEmailTokenCommand>
    {
        public GenerateChangeEmailTokenValidator()
        {
            RuleFor(x => x.GenerateChangeEmailToken.NewEmail)
                .NotEmpty().WithMessage("New email address is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        }
    }
}