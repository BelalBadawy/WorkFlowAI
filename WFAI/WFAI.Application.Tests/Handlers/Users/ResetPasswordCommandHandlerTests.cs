using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Users;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var request = TestData.ResetPasswordRequest();
        var command = new ResetPasswordCommand { ResetPasswordRequest = request };
        var expected = ResponseWrapper.Success("Password reset successfully.");

        _userService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(expected);

        var handler = new ResetPasswordCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(service => service.ResetPasswordAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        var request = TestData.ResetPasswordRequest();
        var command = new ResetPasswordCommand { ResetPasswordRequest = request };
        var expected = ResponseWrapper.Fail("Invalid token.", 400);

        _userService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(expected);

        var handler = new ResetPasswordCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Invalid token.");
        result.StatusCode.Should().Be(400);
        _userService.Verify(service => service.ResetPasswordAsync(request), Times.Once);
    }
}