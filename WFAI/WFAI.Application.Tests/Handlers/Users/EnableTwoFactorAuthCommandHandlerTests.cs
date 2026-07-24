using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands.EnableTwoFactorAuth;
using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Tests.Handlers.Users;

public class EnableTwoFactorAuthCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_WhenCalled_CallsUserServiceEnableWithCorrectRequest()
    {
        var request = new TwoFactorCodeRequest { Code = "123456" };
        var command = new EnableTwoFactorAuthCommand { Request = request };

        _userService
            .Setup(s => s.EnableTwoFactorAuthAsync(request))
            .ReturnsAsync(ResponseWrapper<List<string>>.Success(["code1", "code2"], "Enabled."));

        var handler = new EnableTwoFactorAuthCommandHandler(_userService.Object);
        await handler.Handle(command, CancellationToken.None);

        _userService.Verify(s => s.EnableTwoFactorAuthAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesThroughRecoveryCodesList()
    {
        var codes = Enumerable.Range(1, 10).Select(i => $"code-{i}").ToList();
        var request = new TwoFactorCodeRequest { Code = "123456" };
        var command = new EnableTwoFactorAuthCommand { Request = request };
        var expected = ResponseWrapper<List<string>>.Success(codes, "Enabled.");

        _userService
            .Setup(s => s.EnableTwoFactorAuthAsync(request))
            .ReturnsAsync(expected);

        var handler = new EnableTwoFactorAuthCommandHandler(_userService.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        result.Data.Should().HaveCount(10);
    }
}