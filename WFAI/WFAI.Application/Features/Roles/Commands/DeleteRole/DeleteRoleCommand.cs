namespace WFAI.Application.Features.Roles.Commands
{
    public class DeleteRoleCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public int RoleId { get; set; }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, IResponseWrapper>
    {
        private readonly IRoleService _roleService;

        public DeleteRoleCommandHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper> Handle(DeleteRoleCommand request, CancellationToken ct)
        {
            return await _roleService.DeleteRoleAsync(request.RoleId);
        }
    }
}