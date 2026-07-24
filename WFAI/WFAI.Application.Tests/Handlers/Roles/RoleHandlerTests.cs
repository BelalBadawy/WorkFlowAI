using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Features.Roles.Queries;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Handlers.Roles;

public class GetRolesQueryHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_return_roles_when_service_finds_matches()
    {
        List<RoleResponse> roles =
        [
            TestData.RoleResponse(1),
            TestData.RoleResponse(2)
        ];
        var expected = ResponseWrapper<List<RoleResponse>>.Success(roles);
        _roleService.Setup(service => service.GetRolesAsync()).ReturnsAsync(expected);
        var handler = new GetRolesQueryHandler(_roleService.Object);

        var result = await handler.Handle(new GetRolesQuery(), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(roles);
        _roleService.Verify(service => service.GetRolesAsync(), Times.Once);
    }
}

public class GetRoleByIdQueryHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_return_role_when_service_finds_match()
    {
        var role = TestData.RoleResponse(15);
        var expected = ResponseWrapper<RoleResponse>.Success(role);
        _roleService.Setup(service => service.GetRoleByIdAsync(role.Id)).ReturnsAsync(expected);
        var handler = new GetRoleByIdQueryHandler(_roleService.Object);

        var result = await handler.Handle(new GetRoleByIdQuery { RoleId = role.Id }, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(role);
        _roleService.Verify(service => service.GetRoleByIdAsync(role.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_should_return_failure_when_service_cannot_find_role()
    {
        const int missingRoleId = 404;
        var expected = ResponseWrapper<RoleResponse>.Fail("Role not found.", 404);
        _roleService.Setup(service => service.GetRoleByIdAsync(missingRoleId)).ReturnsAsync(expected);
        var handler = new GetRoleByIdQueryHandler(_roleService.Object);

        var result = await handler.Handle(new GetRoleByIdQuery { RoleId = missingRoleId }, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Role not found.");
        result.StatusCode.Should().Be(404);
    }
}

public class GetPermissionsQueryHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_return_permissions_when_service_finds_match()
    {
        var response = TestData.RoleClaimResponse(7);
        var expected = ResponseWrapper<RoleClaimResponse>.Success(response);
        _roleService.Setup(service => service.GetPermissionsAsync(7)).ReturnsAsync(expected);
        var handler = new GetPermissionsQueryHandler(_roleService.Object);

        var result = await handler.Handle(new GetPermissionsQuery { RoleId = 7 }, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(response);
        _roleService.Verify(service => service.GetPermissionsAsync(7), Times.Once);
    }
}

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_delegate_to_role_service_and_return_success_response()
    {
        var request = TestData.CreateRoleRequest();
        var expected = ResponseWrapper.Success("Role created successfully.");
        _roleService.Setup(service => service.CreateRoleAsync(request)).ReturnsAsync(expected);
        var handler = new CreateRoleCommandHandler(_roleService.Object);

        var result = await handler.Handle(new CreateRoleCommand { CreateRole = request }, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _roleService.Verify(service => service.CreateRoleAsync(request), Times.Once);
    }
}

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_delegate_to_role_service_and_return_success_response()
    {
        var request = TestData.UpdateRoleRequest();
        var expected = ResponseWrapper.Success("Role updated successfully.");
        _roleService.Setup(service => service.UpdateRoleAsync(request)).ReturnsAsync(expected);
        var handler = new UpdateRoleCommandHandler(_roleService.Object);

        var result = await handler.Handle(new UpdateRoleCommand { UpdateRole = request }, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _roleService.Verify(service => service.UpdateRoleAsync(request), Times.Once);
    }
}

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_delegate_to_role_service_and_return_success_response()
    {
        const int roleId = 9;
        var expected = ResponseWrapper.Success("Role deleted successfully.");
        _roleService.Setup(service => service.DeleteRoleAsync(roleId)).ReturnsAsync(expected);
        var handler = new DeleteRoleCommandHandler(_roleService.Object);

        var result = await handler.Handle(new DeleteRoleCommand { RoleId = roleId }, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _roleService.Verify(service => service.DeleteRoleAsync(roleId), Times.Once);
    }
}

public class UpdateRolePermissionsCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    [Fact]
    public async Task Handle_should_delegate_to_role_service_and_return_success_response()
    {
        var request = TestData.UpdateRoleClaimsRequest();
        var expected = ResponseWrapper.Success("Role permissions updated successfully.");
        _roleService.Setup(service => service.UpdateRolePermissionsAsync(request)).ReturnsAsync(expected);
        var handler = new UpdateRolePermissionsCommandHandler(_roleService.Object);

        var result = await handler.Handle(
            new UpdateRolePermissionsCommand { UpdateRoleClaims = request },
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        _roleService.Verify(service => service.UpdateRolePermissionsAsync(request), Times.Once);
    }
}