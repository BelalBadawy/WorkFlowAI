namespace WFAI.Application.Features.Users.Commands
{
    public class ChangeUserStatusCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ChangeUserStatusRequest ChangeUserStatus { get; set; }
    }

    public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ChangeUserStatusCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(ChangeUserStatusCommand request, CancellationToken ct)
        {
            return await _userService.ChangeUserStatusAsync(request.ChangeUserStatus);
        }
    }
}