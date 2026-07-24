namespace WFAI.Application.Features.Token.Queries.LoginWith2FA
{
    public class LoginWith2FAQuery
        : IRequest<IResponseWrapper<TokenResponse>>, IValidateMe
    {
        public TwoFactorLoginRequest Request { get; set; } = null!;
    }

    public class LoginWith2FAQueryHandler
        : IRequestHandler<LoginWith2FAQuery, IResponseWrapper<TokenResponse>>
    {
        private readonly ITokenService _tokenService;

        public LoginWith2FAQueryHandler(ITokenService tokenService)
            => _tokenService = tokenService;

        public async ValueTask<IResponseWrapper<TokenResponse>> Handle(
            LoginWith2FAQuery request, CancellationToken ct)
            => await _tokenService.LoginWith2FAAsync(request.Request, ct);
    }
}