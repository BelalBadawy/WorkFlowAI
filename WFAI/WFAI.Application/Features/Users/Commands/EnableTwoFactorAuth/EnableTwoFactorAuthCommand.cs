using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Features.Users.Commands.EnableTwoFactorAuth
{
    public class EnableTwoFactorAuthCommand
        : IRequest<IResponseWrapper<List<string>>>, IValidateMe
    {
        public TwoFactorCodeRequest Request { get; set; } = null!;
    }

    public class EnableTwoFactorAuthCommandHandler
        : IRequestHandler<EnableTwoFactorAuthCommand, IResponseWrapper<List<string>>>
    {
        private readonly IUserService _userService;

        public EnableTwoFactorAuthCommandHandler(IUserService userService)
            => _userService = userService;

        public async ValueTask<IResponseWrapper<List<string>>> Handle(
            EnableTwoFactorAuthCommand request, CancellationToken ct)
            => await _userService.EnableTwoFactorAuthAsync(request.Request);
    }
}