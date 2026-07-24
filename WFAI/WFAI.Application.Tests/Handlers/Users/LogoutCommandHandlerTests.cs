using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands.Logout;

namespace WFAI.Application.Tests.Handlers.Users;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_WhenCalled_CallsUserServiceLogoutWithCorrectRequest()
    {
        var request = new LogoutRequest { RefreshToken = "token-abc" };
        var command = new LogoutCommand { Request = request };

        _userService
            .Setup(s => s.LogoutAsync(request))
            .ReturnsAsync(ResponseWrapper.Success("Logged out successfully."));

        var handler = new LogoutCommandHandler(_userService.Object);
        await handler.Handle(command, CancellationToken.None);

        _userService.Verify(s => s.LogoutAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesThroughServiceResult()
    {
        var request = new LogoutRequest { RefreshToken = "token-abc" };
        var command = new LogoutCommand { Request = request };
        var expected = ResponseWrapper.Success("Logged out successfully.");

        _userService
            .Setup(s => s.LogoutAsync(request))
            .ReturnsAsync(expected);

        var handler = new LogoutCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}