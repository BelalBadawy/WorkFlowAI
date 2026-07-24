namespace WFAI.Application.Features.Users.Commands.Logout
{
    public class LogoutCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public LogoutRequest Request { get; set; } = null!;
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public LogoutCommandHandler(IUserService userService)
            => _userService = userService;

        public async ValueTask<IResponseWrapper> Handle(
            LogoutCommand request, CancellationToken ct)
            => await _userService.LogoutAsync(request.Request);
    }
}