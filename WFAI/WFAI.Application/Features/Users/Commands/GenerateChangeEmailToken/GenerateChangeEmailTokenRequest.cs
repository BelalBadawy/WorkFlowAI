namespace WFAI.Application.Features.Users.Commands
{
    public class GenerateChangeEmailTokenRequest
    {
        public string NewEmail { get; set; } = string.Empty;
    }
}