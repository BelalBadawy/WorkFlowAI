using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;

namespace WFAI.Application.Tests.Handlers.Users;

public class GenerateNew2FARecoveryCodesCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_delegate_to_user_service_and_return_codes()
    {
        var codes = new List<string> { "code1", "code2" };
        var expected = ResponseWrapper<List<string>>.Success(codes, "New recovery codes generated.");

        _userService.Setup(s => s.GenerateNew2FARecoveryCodesAsync()).ReturnsAsync(expected);

        var handler = new GenerateNew2FARecoveryCodesCommandHandler(_userService.Object);
        var result = await handler.Handle(new GenerateNew2FARecoveryCodesCommand(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        _userService.Verify(s => s.GenerateNew2FARecoveryCodesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_should_propagate_failure_without_wrapping()
    {
        var expected = ResponseWrapper<List<string>>.Fail("Two-factor authentication is not enabled.");

        _userService.Setup(s => s.GenerateNew2FARecoveryCodesAsync()).ReturnsAsync(expected);

        var handler = new GenerateNew2FARecoveryCodesCommandHandler(_userService.Object);
        var result = await handler.Handle(new GenerateNew2FARecoveryCodesCommand(), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Two-factor authentication is not enabled.");
    }
}