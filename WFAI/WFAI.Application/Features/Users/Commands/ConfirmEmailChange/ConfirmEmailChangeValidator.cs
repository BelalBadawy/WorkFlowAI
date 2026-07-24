namespace WFAI.Application.Features.Users.Commands
{
    public class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeCommand>
    {
        public ConfirmEmailChangeValidator()
        {
            RuleFor(x => x.ConfirmEmailChange.UserId)
                .NotEqual(0).WithMessage("User Id is required.");

            RuleFor(x => x.ConfirmEmailChange.NewEmail)
                .NotEmpty().WithMessage("New email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.ConfirmEmailChange.Token)
                .NotEmpty().WithMessage("Token is required.");
        }
    }
}