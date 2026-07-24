namespace WFAI.Application.Features.Users.Commands
{
    public class UnlockUserCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public UnlockUserRequest UnlockUser { get; set; }
    }

    public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public UnlockUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(UnlockUserCommand request, CancellationToken ct)
        {
            return await _userService.UnlockUserAsync(request.UnlockUser.UserId);
        }
    }
}