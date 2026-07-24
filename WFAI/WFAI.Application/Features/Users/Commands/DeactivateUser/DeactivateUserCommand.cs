using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Features.Users.Commands.DeactivateUser
{
    public record DeactivateUserCommand(int UserId) : ICommand<IResponseWrapper>, IValidateMe;

    public class DeactivateUserCommandHandler(IUserService userService)
        : ICommandHandler<DeactivateUserCommand, IResponseWrapper>
    {
        private readonly IUserService _userService = userService;

        public async ValueTask<IResponseWrapper> Handle(DeactivateUserCommand request, CancellationToken ct)
        {
            var statusRequest = new ChangeUserStatusRequest
            {
                UserId = request.UserId,
                ActivateOrDeactivate = false
            };

            return await _userService.ChangeUserStatusAsync(statusRequest);
        }
    }
}