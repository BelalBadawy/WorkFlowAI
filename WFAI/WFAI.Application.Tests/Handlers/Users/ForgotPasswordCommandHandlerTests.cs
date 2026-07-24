using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        const string email = "user@example.com";
        var command = new ForgotPasswordCommand { Email = email };
        var expected = ResponseWrapper.Success("Password reset link sent.");

        _userService
            .Setup(service => service.ForgotPasswordAsync(email))
            .ReturnsAsync(expected);

        var handler = new ForgotPasswordCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(service => service.ForgotPasswordAsync(email), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        const string email = "missing@example.com";
        var command = new ForgotPasswordCommand { Email = email };
        var expected = ResponseWrapper.Fail("User not found.", 404);

        _userService
            .Setup(service => service.ForgotPasswordAsync(email))
            .ReturnsAsync(expected);

        var handler = new ForgotPasswordCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User not found.");
        result.StatusCode.Should().Be(404);
        _userService.Verify(service => service.ForgotPasswordAsync(email), Times.Once);
    }
}