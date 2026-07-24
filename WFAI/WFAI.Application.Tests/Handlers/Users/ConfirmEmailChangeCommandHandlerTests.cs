using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class ConfirmEmailChangeCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var req = new ConfirmEmailChangeRequest { UserId = 1, NewEmail = "new@test.com", Token = "tok" };
        var command = new ConfirmEmailChangeCommand { ConfirmEmailChange = req };
        var expected = ResponseWrapper.Success("Email changed successfully.");

        _userService.Setup(s => s.ConfirmEmailChangeAsync(1, "new@test.com", "tok")).ReturnsAsync(expected);

        var handler = new ConfirmEmailChangeCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.ConfirmEmailChangeAsync(1, "new@test.com", "tok"), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var req = new ConfirmEmailChangeRequest { UserId = 99, NewEmail = "x@x.com", Token = "bad" };
        var command = new ConfirmEmailChangeCommand { ConfirmEmailChange = req };
        var expected = ResponseWrapper.Fail("User does not exist.");

        _userService.Setup(s => s.ConfirmEmailChangeAsync(99, "x@x.com", "bad")).ReturnsAsync(expected);

        var handler = new ConfirmEmailChangeCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User does not exist.");
    }
}