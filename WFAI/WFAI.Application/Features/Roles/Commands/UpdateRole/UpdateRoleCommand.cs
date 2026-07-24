namespace WFAI.Application.Features.Roles.Commands
{
    public class UpdateRoleRequest
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateRoleCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public UpdateRoleRequest UpdateRole { get; set; }
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, IResponseWrapper>
    {
        private readonly IRoleService _roleService;

        public UpdateRoleCommandHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper> Handle(UpdateRoleCommand request, CancellationToken ct)
        {
            return await _roleService.UpdateRoleAsync(request.UpdateRole);
        }
    }
}