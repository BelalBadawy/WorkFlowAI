namespace WFAI.Application.Features.Users.Commands
{
    public class UpdateUserRolesCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public UpdateUserRolesRequest UpdateUserRoles { get; set; }
    }

    public class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public UpdateUserRolesCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(UpdateUserRolesCommand request, CancellationToken ct)
        {
            return await _userService.UpdateUserRolesAsync(request.UpdateUserRoles, ct);
        }
    }
}