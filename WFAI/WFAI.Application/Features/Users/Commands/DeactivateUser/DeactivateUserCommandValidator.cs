using FluentValidation;

namespace WFAI.Application.Features.Users.Commands.DeactivateUser
{
    public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
    {
        public DeactivateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be greater than 0.");
        }
    }
}