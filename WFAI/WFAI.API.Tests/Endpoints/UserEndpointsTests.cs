using System.Net;
using System.Net.Http.Json;
using WFAI.Application.Authorization;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class UserEndpointsTests : ApiTestBase
{
    public UserEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private void UseUserClient(string authMode, string requiredPermission)
    {
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
    }

    [Fact]
    public async Task Get_user_by_id_should_return_successful_response_when_user_exists()
    {
        var email = $"get-user-{Guid.NewGuid():N}@example.com";
        var seededUser = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Read));

        var response = await Client.GetAsync($"/api/v1/users/{seededUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<UserResponseContract>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(seededUser.Id);
        payload.Data.Email.Should().Be(email);
    }

    [Fact]
    public async Task Get_user_by_id_should_return_not_found_when_user_does_not_exist()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Read));

        var response = await Client.GetAsync("/api/v1/users/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("low-privilege")]
    [InlineData("privileged")]
    public async Task Register_user_should_be_accessible_to_all_roles_since_it_is_anonymous(string authMode)
    {
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Create);
        UseUserClient(authMode, requiredPermission);

        var email = $"register-{Guid.NewGuid():N}@example.com";
        var request = new
        {
            FullName = "Registered User",
            Email = email,
            Password = "Admin@123",
            ConfirmPassword = "Admin@123",
            PhoneNumber = "01000000000",
            AutoConfirmEmail = true,
            ActivateUser = true
        };

        var response = await Client.PostAsJsonAsync("/api/v1/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = "Admin@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Get_users_paged_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        await Seeder.SeedUserAsync($"paged-users-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Read);
        UseUserClient(authMode, requiredPermission);

        var response = await Client.GetAsync("/api/v1/users/paged?pageNumber=1&pageSize=10&sortBy=fullname&sortDirection=asc");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<PagedResultContract<UserResponseContract>>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.CurrentPage.Should().Be(1);
        payload.Data.PageSize.Should().Be(10);
        payload.Data.Data.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Update_user_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var user = await Seeder.SeedUserAsync($"update-user-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Update);
        UseUserClient(authMode, requiredPermission);

        var request = new
        {
            UserId = user.Id,
            FullName = "Updated User Name",
            PhoneNumber = "01111111111"
        };

        var response = await Client.PutAsJsonAsync("/api/v1/users/update", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var updatedUser = await Verifier.GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FullName.Should().Be(request.FullName);
        updatedUser.PhoneNumber.Should().Be(request.PhoneNumber);
    }

    [Fact]
    public async Task Change_password_returns_unauthorized_when_not_authenticated()
    {
        var email = $"password-user-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);

        var request = new
        {
            CurrentPassword = "Admin@123",
            NewPassword = "NewPassword@123",
            ConfirmedNewPassword = "NewPassword@123"
        };

        var response = await Client.PutAsJsonAsync("/api/v1/users/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_succeeds_when_user_changes_own_password()
    {
        var email = $"password-self-{Guid.NewGuid():N}@example.com";
        const string currentPassword = "Admin@123";
        const string newPassword = "NewPassword@123";
        var user = await Seeder.SeedUserAsync(email, currentPassword, ["Basic"]);

        UseSelfServiceClient(user.Id);

        var request = new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmedNewPassword = newPassword
        };

        var response = await Client.PutAsJsonAsync("/api/v1/users/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        UseAnonymousClient();
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = newPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Change_user_status_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var user = await Seeder.SeedUserAsync($"status-user-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Update);
        UseUserClient(authMode, requiredPermission);

        var request = new
        {
            UserId = user.Id,
            ActivateOrDeactivate = false
        };

        var response = await Client.PutAsJsonAsync("/api/v1/users/change-status", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var updatedUser = await Verifier.GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Deactivate_user_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var user = await Seeder.SeedUserAsync($"deactivate-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Update);
        UseUserClient(authMode, requiredPermission);

        var response = await Client.PutAsync($"/api/v1/users/{user.Id}/deactivate", null);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var updatedUser = await Verifier.GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Update_user_roles_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"UserRole-{Guid.NewGuid():N}", "Assigned role");
        var user = await Seeder.SeedUserAsync($"roles-user-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Update);
        UseUserClient(authMode, requiredPermission);

        var request = new
        {
            UserId = user.Id,
            Roles = new[] { role.Name! }
        };

        var response = await Client.PutAsJsonAsync("/api/v1/users/user-roles", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var roleNames = await Verifier.GetUserRoleNamesAsync(user.Id);
        roleNames.Should().Contain(role.Name!);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Get_user_roles_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var role = await Seeder.SeedRoleAsync($"ReadRole-{Guid.NewGuid():N}", "Readable role");
        var user = await Seeder.SeedUserAsync($"get-roles-user-{Guid.NewGuid():N}@example.com", "Admin@123", [role.Name!]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Read);
        UseUserClient(authMode, requiredPermission);

        var response = await Client.GetAsync($"/api/v1/users/roles/{user.Id}");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<List<UserRoleContract>>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data.Should().Contain(roleContract => roleContract.RoleName == role.Name);
    }

    [Fact]
    public async Task Generate_change_email_token_returns_unauthorized_when_anonymous()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/users/generate-change-email-token", new
        {
            NewEmail = "new@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_change_email_token_returns_error_when_authenticated_user_not_in_db()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.ChangeEmail));

        var response = await Client.PostAsJsonAsync("/api/v1/users/generate-change-email-token", new
        {
            NewEmail = "new@example.com"
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task Generate_2fa_recovery_codes_returns_unauthorized_when_anonymous()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/users/generate-2fa-recovery-codes", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_2fa_recovery_codes_returns_error_when_authenticated_user_not_in_db()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Manage2FA));

        var response = await Client.PostAsJsonAsync("/api/v1/users/generate-2fa-recovery-codes", new { });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Lock_user_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var user = await Seeder.SeedUserAsync($"lock-user-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Lock);
        UseUserClient(authMode, requiredPermission);

        var response = await Client.PutAsJsonAsync("/api/v1/users/lock-user", new
        {
            UserId = user.Id
        });

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Unlock_user_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var user = await Seeder.SeedUserAsync($"unlock-user-{Guid.NewGuid():N}@example.com", "Admin@123", ["Basic"]);
        var requiredPermission = AppPermission.NameFor(AppService.Identity, AppFeature.Users, AppAction.Unlock);
        UseUserClient(authMode, requiredPermission);

        var response = await Client.PutAsJsonAsync("/api/v1/users/unlock-user", new
        {
            UserId = user.Id
        });

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }
}