namespace WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth;

public class DisableTwoFactorAuthRequest
{
    public string Password { get; set; } = string.Empty;
    public string? Code { get; set; }
}