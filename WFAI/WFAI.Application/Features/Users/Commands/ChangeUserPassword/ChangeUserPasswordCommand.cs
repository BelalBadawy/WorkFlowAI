using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Users.Commands
{
    public class ChangeUserPasswordCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ChangePasswordRequest ChangePassword { get; set; }
    }

    public class ChangeUserPasswordCommandHandler : IRequestHandler<ChangeUserPasswordCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public ChangeUserPasswordCommandHandler(IUserService userService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }

        public async ValueTask<IResponseWrapper> Handle(ChangeUserPasswordCommand request, CancellationToken ct)
        {
            var userId = _currentUserService.GetUserId();
            if (userId is null)
                return ResponseWrapper.Fail("User is not authenticated.");

            return await _userService.ChangeUserPasswordAsync(userId.Value, request.ChangePassword);
        }
    }
}