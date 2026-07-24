
namespace WFAI.Application.Features.Roles
{
    public class RoleClaimResponse
    {
        public RoleResponse Role { get; set; }
        public List<RoleClaimViewModel> RoleClaims { get; set; }
    }
}