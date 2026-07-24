namespace WFAI.Application.Features.Users.Commands
{
    public class LockUserValidator : AbstractValidator<LockUserCommand>
    {
        public LockUserValidator()
        {
            RuleFor(x => x.LockUser.UserId)
                .NotEqual(0).WithMessage("User Id is required.");
        }
    }
}