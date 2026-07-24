using Moq;
using FluentAssertions;
using Xunit;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Features.Users.Commands.DeactivateUser;

namespace WFAI.Application.Tests.Handlers.Users
{
    public class DeactivateUserCommandHandlerTests
    {
        private readonly Mock<IUserService> _userService = new();

        [Fact]
        public async Task Handle_should_delegate_deactivation_to_user_service_and_return_success_response()
        {
            var userId = 5;
            var command = new DeactivateUserCommand(userId);
            var expected = ResponseWrapper.Success("User de-activated successfully");

            _userService
                .Setup(service => service.ChangeUserStatusAsync(It.Is<ChangeUserStatusRequest>(r => r.UserId == userId && !r.ActivateOrDeactivate)))
                .ReturnsAsync(expected);

            var handler = new DeactivateUserCommandHandler(_userService.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeSameAs(expected);
            _userService.Verify(service => service.ChangeUserStatusAsync(It.Is<ChangeUserStatusRequest>(r => r.UserId == userId && !r.ActivateOrDeactivate)), Times.Once);
        }

        [Fact]
        public async Task Handle_should_propagate_failure_response_when_deactivation_fails()
        {
            var userId = 5;
            var command = new DeactivateUserCommand(userId);
            var expected = ResponseWrapper.Fail("User not found.", 404);

            _userService
                .Setup(service => service.ChangeUserStatusAsync(It.Is<ChangeUserStatusRequest>(r => r.UserId == userId && !r.ActivateOrDeactivate)))
                .ReturnsAsync(expected);

            var handler = new DeactivateUserCommandHandler(_userService.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Messages.Should().Contain("User not found.");
            result.StatusCode.Should().Be(404);
            _userService.Verify(service => service.ChangeUserStatusAsync(It.Is<ChangeUserStatusRequest>(r => r.UserId == userId && !r.ActivateOrDeactivate)), Times.Once);
        }
    }
}