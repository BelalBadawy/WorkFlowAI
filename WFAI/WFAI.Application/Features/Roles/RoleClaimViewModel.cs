namespace WFAI.Application.Features.Roles
{
    public class RoleClaimViewModel
    {
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}