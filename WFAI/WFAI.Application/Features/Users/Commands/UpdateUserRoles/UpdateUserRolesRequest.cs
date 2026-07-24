namespace WFAI.Application.Features.Users.Commands
{
    public class UpdateUserRolesRequest
    {
        public int UserId { get; set; }
        public List<string> Roles { get; set; }
    }
}