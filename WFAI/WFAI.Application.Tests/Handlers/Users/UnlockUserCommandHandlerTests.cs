using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class UnlockUserCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var command = new UnlockUserCommand { UnlockUser = new UnlockUserRequest { UserId = 5 } };
        var expected = ResponseWrapper.Success("User unlocked successfully.");

        _userService.Setup(s => s.UnlockUserAsync(5)).ReturnsAsync(expected);

        var handler = new UnlockUserCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.UnlockUserAsync(5), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var command = new UnlockUserCommand { UnlockUser = new UnlockUserRequest { UserId = 99 } };
        var expected = ResponseWrapper.Fail("User does not exist.");

        _userService.Setup(s => s.UnlockUserAsync(99)).ReturnsAsync(expected);

        var handler = new UnlockUserCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User does not exist.");
    }
}