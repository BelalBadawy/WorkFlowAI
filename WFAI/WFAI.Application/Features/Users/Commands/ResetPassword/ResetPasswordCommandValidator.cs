namespace WFAI.Application.Features.Users.Commands
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            // Rule for Token
            RuleFor(x => x.ResetPasswordRequest.Token)
                .NotEmpty().WithMessage("Token is required.");

            RuleFor(x => x.ResetPasswordRequest.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.ResetPasswordRequest.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                // Optional stronger rules â€” uncomment if you want complexity checks:
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
                ;

            RuleFor(x => x.ResetPasswordRequest.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.ResetPasswordRequest.Password).WithMessage("Passwords do not match.");

        }
    }
}