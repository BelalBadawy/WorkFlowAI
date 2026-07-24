namespace WFAI.Application.Features.Users.Commands
{
    public class GenerateNew2FARecoveryCodesCommand : IRequest<IResponseWrapper<List<string>>>
    {
    }

    public class GenerateNew2FARecoveryCodesCommandHandler
        : IRequestHandler<GenerateNew2FARecoveryCodesCommand, IResponseWrapper<List<string>>>
    {
        private readonly IUserService _userService;

        public GenerateNew2FARecoveryCodesCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper<List<string>>> Handle(
            GenerateNew2FARecoveryCodesCommand request, CancellationToken ct)
        {
            return await _userService.GenerateNew2FARecoveryCodesAsync();
        }
    }
}