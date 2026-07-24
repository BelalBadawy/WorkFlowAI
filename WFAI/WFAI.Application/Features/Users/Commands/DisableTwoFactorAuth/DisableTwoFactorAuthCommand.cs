namespace WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth
{
    public class DisableTwoFactorAuthCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public DisableTwoFactorAuthRequest Request { get; set; } = null!;
    }

    public class DisableTwoFactorAuthCommandHandler
        : IRequestHandler<DisableTwoFactorAuthCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public DisableTwoFactorAuthCommandHandler(IUserService userService)
            => _userService = userService;

        public async ValueTask<IResponseWrapper> Handle(
            DisableTwoFactorAuthCommand request, CancellationToken ct)
            => await _userService.DisableTwoFactorAuthAsync(request.Request);
    }
}