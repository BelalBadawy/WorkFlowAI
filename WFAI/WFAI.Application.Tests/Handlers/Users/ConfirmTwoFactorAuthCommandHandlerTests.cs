using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands.ConfirmTwoFactorAuth;
using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Tests.Handlers.Users;

public class ConfirmTwoFactorAuthCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_WhenCalled_CallsUserServiceConfirmWithCorrectRequest()
    {
        var request = new TwoFactorCodeRequest { Code = "123456" };
        var command = new ConfirmTwoFactorAuthCommand { Request = request };

        _userService
            .Setup(s => s.ConfirmTwoFactorAuthAsync(request))
            .ReturnsAsync(ResponseWrapper.Success("Verification code is valid."));

        var handler = new ConfirmTwoFactorAuthCommandHandler(_userService.Object);
        await handler.Handle(command, CancellationToken.None);

        _userService.Verify(s => s.ConfirmTwoFactorAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesThroughServiceResult()
    {
        var request = new TwoFactorCodeRequest { Code = "123456" };
        var command = new ConfirmTwoFactorAuthCommand { Request = request };
        var expected = ResponseWrapper.Success("Verification code is valid.");

        _userService
            .Setup(s => s.ConfirmTwoFactorAuthAsync(request))
            .ReturnsAsync(expected);

        var handler = new ConfirmTwoFactorAuthCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}