namespace WFAI.Application.Features.Users.Commands
{
    public class UnlockUserValidator : AbstractValidator<UnlockUserCommand>
    {
        public UnlockUserValidator()
        {
            RuleFor(x => x.UnlockUser.UserId)
                .NotEqual(0).WithMessage("User Id is required.");
        }
    }
}