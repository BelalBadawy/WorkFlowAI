using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class GenerateChangeEmailTokenCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_success_response()
    {
        var command = new GenerateChangeEmailTokenCommand { GenerateChangeEmailToken = new GenerateChangeEmailTokenRequest { NewEmail = "new@test.com" } };
        var expected = ResponseWrapper.Success("Email change confirmation sent. Please check your inbox.");

        _userService.Setup(s => s.GenerateChangeEmailTokenAsync("new@test.com")).ReturnsAsync(expected);

        var handler = new GenerateChangeEmailTokenCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.GenerateChangeEmailTokenAsync("new@test.com"), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var command = new GenerateChangeEmailTokenCommand { GenerateChangeEmailToken = new GenerateChangeEmailTokenRequest { NewEmail = "same@test.com" } };
        var expected = ResponseWrapper.Fail("New email must be different from your current email.");

        _userService.Setup(s => s.GenerateChangeEmailTokenAsync("same@test.com")).ReturnsAsync(expected);

        var handler = new GenerateChangeEmailTokenCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("New email must be different from your current email.");
    }
}