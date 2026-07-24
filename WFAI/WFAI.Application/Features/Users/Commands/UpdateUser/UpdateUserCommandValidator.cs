namespace WFAI.Application.Features.Users.Commands
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.UpdateUser.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.UpdateUser.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(x => x.UpdateUser.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^[0-9+\-\s]+$").WithMessage("Phone number contains invalid characters.")
                .MinimumLength(11).WithMessage("Phone number must be at least 11 digits long.");
        }
    }
}