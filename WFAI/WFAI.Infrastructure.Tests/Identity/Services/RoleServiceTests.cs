using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Tests.Support;

namespace WFAI.Infrastructure.Tests.Identity.Services;

public class RoleServiceTests : IDisposable
{
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IApplicationDbContext> _context;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _roleManager = IdentityMockFactory.CreateRoleManager();
        _userManager = IdentityMockFactory.CreateUserManager();
        _context = new Mock<IApplicationDbContext>();
        _sut = new RoleService(_roleManager.Object, _userManager.Object, _context.Object);
    }

    public void Dispose() { }

    private static ApplicationRole MakeRole(int id = 1, string name = "TestRole", string description = "Desc") =>
        new() { Id = id, Name = name, Description = description };

    // â”€â”€ CreateRoleAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CreateRoleAsync_WhenRoleAlreadyExists_ReturnsFail()
    {
        var existing = MakeRole();
        _roleManager.Setup(m => m.FindByNameAsync(existing.Name!)).ReturnsAsync(existing);

        var result = await _sut.CreateRoleAsync(new CreateRoleRequest { Name = existing.Name!, Description = "x" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role already exists");
    }

    [Fact]
    public async Task CreateRoleAsync_WhenCreateFails_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByNameAsync("NewRole")).ReturnsAsync((ApplicationRole?)null);
        _roleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Store error." }));

        var result = await _sut.CreateRoleAsync(new CreateRoleRequest { Name = "NewRole", Description = "x" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Store error.");
    }

    [Fact]
    public async Task CreateRoleAsync_WhenSuccessful_ReturnsSuccess()
    {
        _roleManager.Setup(m => m.FindByNameAsync("Manager")).ReturnsAsync((ApplicationRole?)null);
        _roleManager.Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == "Manager")))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.CreateRoleAsync(new CreateRoleRequest { Name = "Manager", Description = "Managers" });

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role created successfully");
    }

    // â”€â”€ DeleteRoleAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task DeleteRoleAsync_WhenRoleIdIsZero_ReturnsFail()
    {
        var result = await _sut.DeleteRoleAsync(0);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role Id is required.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenRoleNotFound_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationRole?)null);

        var result = await _sut.DeleteRoleAsync(99);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role does not exist.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenAdminRole_ReturnsFail()
    {
        var adminRole = MakeRole(1, "Admin");
        _roleManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(adminRole);

        var result = await _sut.DeleteRoleAsync(1);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot delete Admin role.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenUsersAssignedToRole_ReturnsFail()
    {
        var role = MakeRole(2, "Editor");
        _roleManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(role);
        _userManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
                    .ReturnsAsync([new ApplicationUser { Id = 1, Email = "u@t.com" }]);

        var result = await _sut.DeleteRoleAsync(2);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Contain("currently assigned");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenDeleteFails_ReturnsFail()
    {
        var role = MakeRole(3, "Temp");
        _roleManager.Setup(m => m.FindByIdAsync("3")).ReturnsAsync(role);
        _userManager.Setup(m => m.GetUsersInRoleAsync("Temp")).ReturnsAsync([]);
        _roleManager.Setup(m => m.DeleteAsync(role))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot delete." }));

        var result = await _sut.DeleteRoleAsync(3);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot delete.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenSuccessful_ReturnsSuccess()
    {
        var role = MakeRole(4, "Temp");
        _roleManager.Setup(m => m.FindByIdAsync("4")).ReturnsAsync(role);
        _userManager.Setup(m => m.GetUsersInRoleAsync("Temp")).ReturnsAsync([]);
        _roleManager.Setup(m => m.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.DeleteRoleAsync(4);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role successfully deleted.");
    }

    // â”€â”€ GetPermissionsAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleNotFound_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByIdAsync("77")).ReturnsAsync((ApplicationRole?)null);

        var result = await _sut.GetPermissionsAsync(77);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role does not exist.");
    }

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleFound_ReturnsAllAppPermissionsWithRoleInfo()
    {
        var role = MakeRole(5, "Viewer");
        _roleManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(role);
        _roleManager.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

        var result = await _sut.GetPermissionsAsync(5);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Role.Id.Should().Be(5);
        result.Data.Role.Name.Should().Be("Viewer");
        result.Data.RoleClaims.Should().NotBeEmpty();
        result.Data.RoleClaims.Should().OnlyContain(rc => rc.ClaimType == "permission");
    }

    // â”€â”€ GetRoleByIdAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetRoleByIdAsync_WhenRoleNotFound_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByIdAsync("50")).ReturnsAsync((ApplicationRole?)null);

        var result = await _sut.GetRoleByIdAsync(50);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role does not exist.");
    }

    [Fact]
    public async Task GetRoleByIdAsync_WhenRoleFound_ReturnsMappedResponse()
    {
        var role = MakeRole(6, "Editor", "Content editors");
        _roleManager.Setup(m => m.FindByIdAsync("6")).ReturnsAsync(role);

        var result = await _sut.GetRoleByIdAsync(6);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Id.Should().Be(6);
        result.Data.Name.Should().Be("Editor");
        result.Data.Description.Should().Be("Content editors");
    }

    // â”€â”€ GetRolesAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetRolesAsync_WhenNoRolesExist_ReturnsFail()
    {
        _roleManager.Setup(m => m.Roles)
                    .Returns(new TestAsyncEnumerable<ApplicationRole>([]));

        var result = await _sut.GetRolesAsync();

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("No roles were found.");
    }

    [Fact]
    public async Task GetRolesAsync_WhenRolesExist_ReturnsMappedList()
    {
        var roles = new List<ApplicationRole> { MakeRole(1, "Admin"), MakeRole(2, "Basic") };
        _roleManager.Setup(m => m.Roles).Returns(new TestAsyncEnumerable<ApplicationRole>(roles));

        var result = await _sut.GetRolesAsync();

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(r => r.Name).Should().BeEquivalentTo("Admin", "Basic");
    }

    // â”€â”€ UpdateRoleAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UpdateRoleAsync_WhenRoleNotFound_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByIdAsync("88")).ReturnsAsync((ApplicationRole?)null);

        var result = await _sut.UpdateRoleAsync(new UpdateRoleRequest { RoleId = 88, Name = "X", Description = "Y" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role does not exist.");
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenAdminRole_ReturnsFail()
    {
        var admin = MakeRole(1, "Admin");
        _roleManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(admin);

        var result = await _sut.UpdateRoleAsync(new UpdateRoleRequest { RoleId = 1, Name = "SuperAdmin", Description = "X" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot update Admin role.");
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenUpdateFails_ReturnsFail()
    {
        var role = MakeRole(7, "Editor");
        _roleManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(role);
        _roleManager.Setup(m => m.UpdateAsync(role))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrent update." }));

        var result = await _sut.UpdateRoleAsync(new UpdateRoleRequest { RoleId = 7, Name = "NewEditor", Description = "X" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Concurrent update.");
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenSuccessful_ReturnsSuccess()
    {
        var role = MakeRole(8, "OldName");
        _roleManager.Setup(m => m.FindByIdAsync("8")).ReturnsAsync(role);
        _roleManager.Setup(m => m.UpdateAsync(It.Is<ApplicationRole>(r => r.Name == "NewName")))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateRoleAsync(new UpdateRoleRequest { RoleId = 8, Name = "NewName", Description = "Updated" });

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role updated successfully");
    }

    // â”€â”€ UpdateRolePermissionsAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenRoleNotFound_ReturnsFail()
    {
        _roleManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationRole?)null);

        var result = await _sut.UpdateRolePermissionsAsync(new UpdateRoleClaimsRequest { RoleId = 99, RoleClaims = [] });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role does not exist.");
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenAdminRole_ReturnsFail()
    {
        var admin = MakeRole(1, "Admin");
        _roleManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(admin);

        var result = await _sut.UpdateRolePermissionsAsync(new UpdateRoleClaimsRequest { RoleId = 1, RoleClaims = [] });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot change permissions for this role.");
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenNoChanges_ReturnsNoChangesMessage()
    {
        var role = MakeRole(9, "Viewer");
        var existingClaims = new List<Claim> { new("permission", "Permission.Identity.Roles.Read") };
        _roleManager.Setup(m => m.FindByIdAsync("9")).ReturnsAsync(role);
        _roleManager.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync(existingClaims);

        var sameClaimsRequest = new UpdateRoleClaimsRequest
        {
            RoleId = 9,
            RoleClaims = [new RoleClaimViewModel { ClaimType = "permission", ClaimValue = "Permission.Identity.Roles.Read" }]
        };

        var result = await _sut.UpdateRolePermissionsAsync(sameClaimsRequest);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("No changes detected.");
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenClaimsAdded_CallsAddClaimAndReturnsSuccess()
    {
        var role = MakeRole(10, "Viewer");
        _roleManager.Setup(m => m.FindByIdAsync("10")).ReturnsAsync(role);
        _roleManager.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([]);
        _roleManager.Setup(m => m.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateRolePermissionsAsync(new UpdateRoleClaimsRequest
        {
            RoleId = 10,
            RoleClaims = [new RoleClaimViewModel { ClaimType = "permission", ClaimValue = "Permission.Identity.Roles.Read" }]
        });

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role permissions updated successfully.");
        _roleManager.Verify(m => m.AddClaimAsync(role, It.Is<Claim>(c => c.Value == "Permission.Identity.Roles.Read")), Times.Once);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenClaimsRemoved_CallsRemoveClaimAndReturnsSuccess()
    {
        var role = MakeRole(11, "Viewer");
        var existingClaims = new List<Claim> { new("permission", "Permission.Identity.Roles.Read") };
        _roleManager.Setup(m => m.FindByIdAsync("11")).ReturnsAsync(role);
        _roleManager.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync(existingClaims);
        _roleManager.Setup(m => m.RemoveClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateRolePermissionsAsync(new UpdateRoleClaimsRequest { RoleId = 11, RoleClaims = [] });

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role permissions updated successfully.");
        _roleManager.Verify(m => m.RemoveClaimAsync(role, It.Is<Claim>(c => c.Value == "Permission.Identity.Roles.Read")), Times.Once);
    }
}