namespace WFAI.Application.Features.Users.Commands
{
    public class LockUserCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public LockUserRequest LockUser { get; set; }
    }

    public class LockUserCommandHandler : IRequestHandler<LockUserCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public LockUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(LockUserCommand request, CancellationToken ct)
        {
            return await _userService.LockUserAsync(request.LockUser.UserId);
        }
    }
}