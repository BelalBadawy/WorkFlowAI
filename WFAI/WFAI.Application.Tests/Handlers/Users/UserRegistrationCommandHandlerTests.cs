using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Users;

public class UserRegistrationCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var request = TestData.UserRegistrationRequest();
        var command = new UserRegistrationCommand { UserRegistration = request };
        var expected = ResponseWrapper.Success("User registered successfully.");

        _userService
            .Setup(service => service.RegisterUserAsync(request))
            .ReturnsAsync(expected);

        var handler = new UserRegistrationCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(service => service.RegisterUserAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_response_without_wrapping_it()
    {
        var request = TestData.UserRegistrationRequest();
        var command = new UserRegistrationCommand { UserRegistration = request };
        var expected = ResponseWrapper.Fail("Email already exists.", 409);

        _userService
            .Setup(service => service.RegisterUserAsync(request))
            .ReturnsAsync(expected);

        var handler = new UserRegistrationCommandHandler(_userService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Email already exists.");
        result.StatusCode.Should().Be(409);
        _userService.Verify(service => service.RegisterUserAsync(request), Times.Once);
    }
}