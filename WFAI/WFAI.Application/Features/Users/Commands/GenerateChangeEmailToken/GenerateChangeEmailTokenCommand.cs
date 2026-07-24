namespace WFAI.Application.Features.Users.Commands
{
    public class GenerateChangeEmailTokenCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public GenerateChangeEmailTokenRequest GenerateChangeEmailToken { get; set; }
    }

    public class GenerateChangeEmailTokenCommandHandler : IRequestHandler<GenerateChangeEmailTokenCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public GenerateChangeEmailTokenCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(GenerateChangeEmailTokenCommand request, CancellationToken ct)
        {
            return await _userService.GenerateChangeEmailTokenAsync(
                request.GenerateChangeEmailToken.NewEmail);
        }
    }
}