using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Features.Users.Commands.ConfirmTwoFactorAuth
{
    public class ConfirmTwoFactorAuthCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public TwoFactorCodeRequest Request { get; set; } = null!;
    }

    public class ConfirmTwoFactorAuthCommandHandler
        : IRequestHandler<ConfirmTwoFactorAuthCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ConfirmTwoFactorAuthCommandHandler(IUserService userService)
            => _userService = userService;

        public async ValueTask<IResponseWrapper> Handle(
            ConfirmTwoFactorAuthCommand request, CancellationToken ct)
            => await _userService.ConfirmTwoFactorAuthAsync(request.Request);
    }
}