namespace WFAI.Application.Features.Users.Commands
{
    public class ChangeUserStatusRequest
    {
        public int UserId { get; set; }
        public bool ActivateOrDeactivate { get; set; }
    }
}