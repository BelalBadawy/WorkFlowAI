namespace WFAI.Application.Features.Token.Queries
{
    public class GetRefreshTokenQuery : IRequest<IResponseWrapper<TokenResponse>>, IValidateMe
    {
        public RefreshTokenRequest RefreshTokenRequest { get; set; }
    }

    public class GetRefreshTokenQueryHandler : IRequestHandler<GetRefreshTokenQuery, IResponseWrapper<TokenResponse>>
    {
        private readonly ITokenService _tokenService;

        public GetRefreshTokenQueryHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async ValueTask<IResponseWrapper<TokenResponse>> Handle(GetRefreshTokenQuery request, CancellationToken ct)
        {
            return await _tokenService.GetRefreshTokenAsync(request.RefreshTokenRequest);
        }
    }
}