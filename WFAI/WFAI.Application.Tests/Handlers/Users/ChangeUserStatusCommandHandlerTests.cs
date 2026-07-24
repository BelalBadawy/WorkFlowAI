using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Users;

public class ChangeUserStatusCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var request = TestData.ChangeUserStatusRequest();
        var command = new ChangeUserStatusCommand { ChangeUserStatus = request };
        var expected = ResponseWrapper.Success("User status updated successfully.");

        _userService
            .Setup(service => service.ChangeUserStatusAsync(request))
            .ReturnsAsync(expected);

        var handler = new ChangeUserStatusCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(service => service.ChangeUserStatusAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        var request = TestData.ChangeUserStatusRequest();
        var command = new ChangeUserStatusCommand { ChangeUserStatus = request };
        var expected = ResponseWrapper.Fail("User not found.", 404);

        _userService
            .Setup(service => service.ChangeUserStatusAsync(request))
            .ReturnsAsync(expected);

        var handler = new ChangeUserStatusCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User not found.");
        result.StatusCode.Should().Be(404);
        _userService.Verify(service => service.ChangeUserStatusAsync(request), Times.Once);
    }
}