using WFAI.Application.Features.Roles.Commands;

namespace WFAI.Application.Features.Roles
{
    public interface IRoleService
    {
        Task<IResponseWrapper> CreateRoleAsync(CreateRoleRequest createRole);
        Task<IResponseWrapper<List<RoleResponse>>> GetRolesAsync( );
        Task<IResponseWrapper> UpdateRoleAsync(UpdateRoleRequest updateRole);
        Task<IResponseWrapper<RoleResponse>> GetRoleByIdAsync(int roleId);
        Task<IResponseWrapper> DeleteRoleAsync(int roleId);
        Task<IResponseWrapper<RoleClaimResponse>> GetPermissionsAsync(int roleId);
        Task<IResponseWrapper> UpdateRolePermissionsAsync(UpdateRoleClaimsRequest updateRoleClaims);
    }
}