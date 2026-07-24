namespace WFAI.Application.Features.Users.Commands
{
    public class ResendConfirmationEmailValidator : AbstractValidator<ResendConfirmationEmailCommand>
    {
        public ResendConfirmationEmailValidator()
        {
            RuleFor(x => x.ResendConfirmation.Email)
                .NotEmpty().WithMessage("Email address is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        }
    }
}