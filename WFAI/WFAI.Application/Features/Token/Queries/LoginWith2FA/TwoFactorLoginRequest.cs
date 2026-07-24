namespace WFAI.Application.Features.Token.Queries.LoginWith2FA;

public class TwoFactorLoginRequest
{
    public string TwoFactorChallengeToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}