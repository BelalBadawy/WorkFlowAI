namespace WFAI.Application.Features.Users.Commands.Logout
{
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.Request.RefreshToken).NotEmpty();
        }
    }
}