namespace WFAI.Application.Features.Users.Commands.Logout;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}