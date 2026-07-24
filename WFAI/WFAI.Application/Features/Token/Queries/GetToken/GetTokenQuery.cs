namespace WFAI.Application.Features.Token.Queries
{
    public class GetTokenQuery : IRequest<IResponseWrapper<TokenResponse>>, IValidateMe
    {
        public TokenRequest TokenRequest { get; set; }
    }

    public class GetTokenQueryHandler : IRequestHandler<GetTokenQuery, IResponseWrapper<TokenResponse>>
    {
        private readonly ITokenService _tokenService;

        public GetTokenQueryHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async ValueTask<IResponseWrapper<TokenResponse>> Handle(GetTokenQuery request, CancellationToken ct)
        {
            return await _tokenService.GetTokenAsync(request.TokenRequest);
        }
    }
}