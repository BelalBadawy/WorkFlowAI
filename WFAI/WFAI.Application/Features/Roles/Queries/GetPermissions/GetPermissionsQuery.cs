namespace WFAI.Application.Features.Roles.Queries
{
    public class GetPermissionsQuery : IRequest<IResponseWrapper<RoleClaimResponse>>, IValidateMe
    {
        public int RoleId { get; set; }
    }

    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IResponseWrapper<RoleClaimResponse>>
    {
        private readonly IRoleService _roleService;

        public GetPermissionsQueryHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async ValueTask<IResponseWrapper<RoleClaimResponse>> Handle(GetPermissionsQuery request, CancellationToken ct)
        {
            return await _roleService.GetPermissionsAsync(request.RoleId);
        }
    }
}