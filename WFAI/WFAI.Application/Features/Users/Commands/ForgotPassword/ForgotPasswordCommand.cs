namespace WFAI.Application.Features.Users.Commands
{
    public class ForgotPasswordCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public string Email { get; set; }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ForgotPasswordCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(ForgotPasswordCommand request, CancellationToken ct)
        {
            return await _userService.ForgotPasswordAsync(request.Email);
        }
    }
}