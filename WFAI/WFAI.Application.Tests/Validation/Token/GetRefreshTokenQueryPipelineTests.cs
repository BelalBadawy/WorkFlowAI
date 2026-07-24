using Moq;
using WFAI.Application.Behaviors;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Token.Queries;

namespace WFAI.Application.Tests.Validation.Token;

public class GetRefreshTokenQueryPipelineTests
{
    [Fact]
    public async Task Handle_should_reject_invalid_refresh_token_query_before_handler_runs()
    {
        var mockFactory = new Mock<IValidationFailureFactory<IResponseWrapper<TokenResponse>>>();
        mockFactory.Setup(f => f.CreateFailure(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>()))
                   .Returns<IReadOnlyList<string>, int>((msgs, code) => ResponseWrapper<TokenResponse>.Fail(msgs, code));
        var behavior = new ValidationPipelineBehavior<GetRefreshTokenQuery, IResponseWrapper<TokenResponse>>(
            [new GetRefreshTokenQueryValidator()], mockFactory.Object);
        var handlerWasCalled = false;
        var query = new GetRefreshTokenQuery
        {
            RefreshTokenRequest = new RefreshTokenRequest
            {
                Token = string.Empty,
                RefreshToken = string.Empty
            }
        };

        var result = await behavior.Handle(
            query,
            (_, _) =>
            {
                handlerWasCalled = true;
                return new ValueTask<IResponseWrapper<TokenResponse>>(
                    ResponseWrapper<TokenResponse>.Success(new TokenResponse()));
            },
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(message => !string.IsNullOrWhiteSpace(message));
        handlerWasCalled.Should().BeFalse();
    }
}