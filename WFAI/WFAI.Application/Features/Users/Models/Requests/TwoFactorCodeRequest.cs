namespace WFAI.Application.Features.Users.Models.Requests;

public class TwoFactorCodeRequest
{
    public string Code { get; set; } = string.Empty;
}