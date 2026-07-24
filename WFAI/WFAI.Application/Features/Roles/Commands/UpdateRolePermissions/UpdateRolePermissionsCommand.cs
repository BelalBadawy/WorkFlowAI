namespace WFAI.Application.Features.Roles.Commands
{
    public class UpdateRoleClaimsRequest
    {
        public int RoleId { get; set; }
        public List<RoleClaimViewModel>? RoleClaims { get; set; }
    }

    public class UpdateRolePermissionsCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public required UpdateRoleClaimsRequest UpdateRoleClaims { get; set; }
    }

    public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, IResponseWrapper>
    {
        private readonly IRoleService _roleService;

        public UpdateRolePermissionsCommandHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper> Handle(UpdateRolePermissionsCommand request, CancellationToken ct)
        {
            return await _roleService.UpdateRolePermissionsAsync(request.UpdateRoleClaims);
        }
    }
}