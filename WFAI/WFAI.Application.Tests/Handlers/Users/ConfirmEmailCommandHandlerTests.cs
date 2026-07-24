using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var command = new ConfirmEmailCommand { ConfirmEmail = new ConfirmEmailRequest { UserId = 1, Token = "tok" } };
        var expected = ResponseWrapper.Success("Email confirmed successfully.");

        _userService.Setup(s => s.ConfirmEmailAsync(1, "tok")).ReturnsAsync(expected);

        var handler = new ConfirmEmailCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.ConfirmEmailAsync(1, "tok"), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var command = new ConfirmEmailCommand { ConfirmEmail = new ConfirmEmailRequest { UserId = 99, Token = "bad" } };
        var expected = ResponseWrapper.Fail("User does not exist.");

        _userService.Setup(s => s.ConfirmEmailAsync(99, "bad")).ReturnsAsync(expected);

        var handler = new ConfirmEmailCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User does not exist.");
    }
}