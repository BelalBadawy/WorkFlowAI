namespace WFAI.Application.Features.Users.Commands
{
    public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(x => x.ConfirmEmail.UserId)
                .NotEqual(0).WithMessage("User Id is required.");

            RuleFor(x => x.ConfirmEmail.Token)
                .NotEmpty().WithMessage("Token is required.");
        }
    }
}