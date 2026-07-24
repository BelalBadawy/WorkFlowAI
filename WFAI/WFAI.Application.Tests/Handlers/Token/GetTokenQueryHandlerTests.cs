using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Token;
using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Token;

public class GetTokenQueryHandlerTests
{
    private readonly Mock<ITokenService> _tokenService = new();

    [Fact]
    public async Task Handle_should_delegate_to_token_service_and_return_success_response()
    {
        var request = TestData.TokenRequest();
        var query = new GetTokenQuery { TokenRequest = request };
        var expected = ResponseWrapper<TokenResponse>.Success(new TokenResponse());

        _tokenService
            .Setup(service => service.GetTokenAsync(request))
            .ReturnsAsync(expected);

        var handler = new GetTokenQueryHandler(_tokenService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _tokenService.Verify(service => service.GetTokenAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        var request = TestData.TokenRequest();
        var query = new GetTokenQuery { TokenRequest = request };
        var expected = ResponseWrapper<TokenResponse>.Fail("Invalid credentials.", 401);

        _tokenService
            .Setup(service => service.GetTokenAsync(request))
            .ReturnsAsync(expected);

        var handler = new GetTokenQueryHandler(_tokenService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Invalid credentials.");
        result.StatusCode.Should().Be(401);
        _tokenService.Verify(service => service.GetTokenAsync(request), Times.Once);
    }
}