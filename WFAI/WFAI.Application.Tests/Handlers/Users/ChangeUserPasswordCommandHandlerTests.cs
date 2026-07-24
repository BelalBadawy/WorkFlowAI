using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Interfaces.Common;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Users;

public class ChangeUserPasswordCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    public ChangeUserPasswordCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.GetUserId()).Returns(42);
    }

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var request = TestData.ChangePasswordRequest();
        var command = new ChangeUserPasswordCommand { ChangePassword = request };
        var expected = ResponseWrapper.Success("Password changed successfully.");

        _userService
            .Setup(service => service.ChangeUserPasswordAsync(42, request))
            .ReturnsAsync(expected);

        var handler = new ChangeUserPasswordCommandHandler(_userService.Object, _currentUserService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(service => service.ChangeUserPasswordAsync(42, request), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        var request = TestData.ChangePasswordRequest();
        var command = new ChangeUserPasswordCommand { ChangePassword = request };
        var expected = ResponseWrapper.Fail("Current password is incorrect.", 400);

        _userService
            .Setup(service => service.ChangeUserPasswordAsync(42, request))
            .ReturnsAsync(expected);

        var handler = new ChangeUserPasswordCommandHandler(_userService.Object, _currentUserService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Current password is incorrect.");
        result.StatusCode.Should().Be(400);
        _userService.Verify(service => service.ChangeUserPasswordAsync(42, request), Times.Once);
    }
}