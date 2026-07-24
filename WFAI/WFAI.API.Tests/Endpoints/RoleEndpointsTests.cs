using System.Net;
using System.Net.Http.Json;
using WFAI.Application.Authorization;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class RoleEndpointsTests : ApiTestBase
{
    public RoleEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_all_roles_should_return_successful_response_when_authorized()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read));

        var response = await Client.GetAsync("/api/v1/roles/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<List<RoleResponseContract>>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_role_should_return_successful_response_when_valid()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Create));

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new
        {
            Name = $"NewTestRole-{suffix}",
            Description = "Test Description"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Get_role_by_id_should_return_not_found_when_id_is_invalid()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read));

        var response = await Client.GetAsync("/api/v1/roles/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Update_role_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"RoleUpdate-{Guid.NewGuid():N}", "Before update");
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Update);

        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var request = new
        {
            RoleId = role.Id,
            Name = $"UpdatedRole-{Guid.NewGuid():N}",
            Description = "Updated description"
        };

        var response = await Client.PutAsJsonAsync("/api/v1/roles", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var updatedRole = await Verifier.GetRoleByIdAsync(role.Id);
        updatedRole.Should().NotBeNull();
        updatedRole!.Name.Should().Be(request.Name);
        updatedRole.Description.Should().Be(request.Description);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Delete_role_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"RoleDelete-{Guid.NewGuid():N}", "Delete me");
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Delete);

        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var response = await Client.DeleteAsync($"/api/v1/roles/{role.Id}");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var deletedRole = await Verifier.GetRoleByIdAsync(role.Id);
        deletedRole.Should().BeNull();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Get_role_permissions_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"RolePerms-{Guid.NewGuid():N}", "Permissions role");
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read);

        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var response = await Client.GetAsync($"/api/v1/roles/permissions/{role.Id}");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<RoleClaimResponseContract>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Role.Id.Should().Be(role.Id);
        payload.Data.RoleClaims.Should().Contain(claim => claim.ClaimValue == AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read));
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Update_role_permissions_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"RolePermUpdate-{Guid.NewGuid():N}", "Update permissions");
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Update);

        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var selectedPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read);
        var request = new
        {
            RoleId = role.Id,
            RoleClaims = new[]
            {
                new
                {
                    ClaimType = "permission",
                    ClaimValue = selectedPermission,
                    Description = "Read Roles"
                }
            }
        };

        var response = await Client.PutAsJsonAsync("/api/v1/roles/update-permissions", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var roleClaims = await Verifier.GetRoleClaimsAsync(role.Id);
        roleClaims.Should().Contain(roleClaim => roleClaim.ClaimValue == selectedPermission);
    }
}