namespace WFAI.Application.Features.Roles.Queries
{
    public class GetRolesQuery : IRequest<IResponseWrapper<List<RoleResponse>>>
    {
    }

    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IResponseWrapper<List<RoleResponse>>>
    {
        private readonly IRoleService _roleService;

        public GetRolesQueryHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper<List<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken ct)
        {
            return await _roleService.GetRolesAsync();
        }
    }
}