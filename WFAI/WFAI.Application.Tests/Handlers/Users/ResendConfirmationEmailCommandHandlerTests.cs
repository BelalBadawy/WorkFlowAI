using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class ResendConfirmationEmailCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var command = new ResendConfirmationEmailCommand { ResendConfirmation = new ResendConfirmationEmailRequest { Email = "user@test.com" } };
        var expected = ResponseWrapper.Success("Confirmation email sent. Please check your inbox.");

        _userService.Setup(s => s.ResendConfirmationEmailAsync("user@test.com")).ReturnsAsync(expected);

        var handler = new ResendConfirmationEmailCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.ResendConfirmationEmailAsync("user@test.com"), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var command = new ResendConfirmationEmailCommand { ResendConfirmation = new ResendConfirmationEmailRequest { Email = "missing@test.com" } };
        var expected = ResponseWrapper.Fail("This email doesn't exist.");

        _userService.Setup(s => s.ResendConfirmationEmailAsync("missing@test.com")).ReturnsAsync(expected);

        var handler = new ResendConfirmationEmailCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("This email doesn't exist.");
    }
}