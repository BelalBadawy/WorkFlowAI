namespace WFAI.Application.Features.Users.Commands
{
    public class ConfirmEmailChangeRequest
    {
        public int    UserId   { get; set; }
        public string NewEmail { get; set; } = string.Empty;
        public string Token    { get; set; } = string.Empty;
    }
}