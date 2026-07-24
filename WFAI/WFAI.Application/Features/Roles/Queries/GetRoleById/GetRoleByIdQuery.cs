namespace WFAI.Application.Features.Roles.Queries
{
    public class GetRoleByIdQuery : IRequest<IResponseWrapper<RoleResponse>>, IValidateMe
    {
        public int RoleId { get; set; }
    }

    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, IResponseWrapper<RoleResponse>>
    {
        private readonly IRoleService _roleService;

        public GetRoleByIdQueryHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper<RoleResponse>> Handle(GetRoleByIdQuery request, CancellationToken ct)
        {
            return await _roleService.GetRoleByIdAsync(request.RoleId);
        }
    }
}