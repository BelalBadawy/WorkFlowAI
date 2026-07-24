namespace WFAI.Application.Features.Users.Commands
{
    public class ChangeUserPasswordValidator : AbstractValidator<ChangeUserPasswordCommand>
    {
        public ChangeUserPasswordValidator()
        {
            RuleFor(x => x.ChangePassword.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.ChangePassword.NewPassword)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                // Optional stronger rules â€” uncomment if you want complexity checks:
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
                ;

            RuleFor(x => x.ChangePassword.ConfirmedNewPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.ChangePassword.NewPassword).WithMessage("Passwords do not match.");
        }
    }
}