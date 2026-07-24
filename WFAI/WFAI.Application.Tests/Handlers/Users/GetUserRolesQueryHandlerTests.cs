using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Models.Requests;
using WFAI.Application.Features.Users.Queries;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Users;

public class GetUserRolesQueryHandlerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Handle_should_return_user_roles_when_service_finds_matches()
    {
        const int userId = 15;
        List<UserRoleViewModel> roles =
        [
            TestData.UserRoleViewModel("Admin"),
            TestData.UserRoleViewModel("Basic")
        ];
        var query = new GetUserRolesQuery { UserId = userId };
        var expected = ResponseWrapper<List<UserRoleViewModel>>.Success(roles);

        _userService
            .Setup(service => service.GetUserRolesAsync(userId))
            .ReturnsAsync(expected);

        var handler = new GetUserRolesQueryHandler(_userService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(roles);
        _userService.Verify(service => service.GetUserRolesAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_should_return_failure_when_service_cannot_find_roles()
    {
        const int missingUserId = 404;
        var query = new GetUserRolesQuery { UserId = missingUserId };
        var expected = ResponseWrapper<List<UserRoleViewModel>>.Fail("User not found.", 404);

        _userService
            .Setup(service => service.GetUserRolesAsync(missingUserId))
            .ReturnsAsync(expected);

        var handler = new GetUserRolesQueryHandler(_userService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("User not found.");
        result.StatusCode.Should().Be(404);
        _userService.Verify(service => service.GetUserRolesAsync(missingUserId), Times.Once);
    }
}