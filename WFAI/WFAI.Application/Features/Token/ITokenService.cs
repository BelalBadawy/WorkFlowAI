using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Features.Token.Queries.LoginWith2FA;

namespace WFAI.Application.Features.Token
{
    public interface ITokenService
    {
        Task<IResponseWrapper<TokenResponse>> GetTokenAsync(TokenRequest tokenRequest);
        Task<IResponseWrapper<TokenResponse>> GetRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest);
        Task<IResponseWrapper<TokenResponse>> LoginWith2FAAsync(TwoFactorLoginRequest request, CancellationToken ct = default);
    }
}