using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands.SetupTwoFactorAuth;
using WFAI.Application.Features.Users.Models.Responses;

namespace WFAI.Application.Tests.Handlers.Users;

public class SetupTwoFactorAuthCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_WhenCalled_CallsUserServiceSetupTwoFactorAuth()
    {
        _userService
            .Setup(s => s.SetupTwoFactorAuthAsync())
            .ReturnsAsync(ResponseWrapper<TwoFactorAuthViewModel>.Success(new TwoFactorAuthViewModel()));

        var handler = new SetupTwoFactorAuthCommandHandler(_userService.Object);
        await handler.Handle(new SetupTwoFactorAuthCommand(), CancellationToken.None);

        _userService.Verify(s => s.SetupTwoFactorAuthAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesThroughViewModelResult()
    {
        var vm = new TwoFactorAuthViewModel { KeySecret = "JBSWY3DPEHPK3PXP", CodeQR = "otpauth://..." };
        var expected = ResponseWrapper<TwoFactorAuthViewModel>.Success(vm);

        _userService
            .Setup(s => s.SetupTwoFactorAuthAsync())
            .ReturnsAsync(expected);

        var handler = new SetupTwoFactorAuthCommandHandler(_userService.Object);
        var result = await handler.Handle(new SetupTwoFactorAuthCommand(), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}