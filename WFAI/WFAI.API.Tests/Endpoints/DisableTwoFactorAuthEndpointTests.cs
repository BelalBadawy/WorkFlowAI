using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class DisableTwoFactorAuthEndpointTests : ApiTestBase
{
    public DisableTwoFactorAuthEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DisableTwoFactorAuth_Unauthenticated_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/users/disable-2fa", new
        {
            Password = "Admin@123",
            Code = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisableTwoFactorAuth_EmptyPassword_ReturnsValidationError()
    {
        var email = $"disable-2fa-val-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PutAsJsonAsync("/api/v1/users/disable-2fa", new
        {
            Password = "",
            Code = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(m => !string.IsNullOrWhiteSpace(m));
    }

    [Fact]
    public async Task DisableTwoFactorAuth_WrongPassword_ReturnsFailure()
    {
        var email = $"disable-2fa-wrong-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PutAsJsonAsync("/api/v1/users/disable-2fa", new
        {
            Password = "WrongPassword@999",
            Code = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task DisableTwoFactorAuth_CorrectPassword_TwoFactorNotEnabled_ReturnsFailure()
    {
        var email = $"disable-2fa-not-enabled-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PutAsJsonAsync("/api/v1/users/disable-2fa", new
        {
            Password = "Admin@123",
            Code = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(m => m.Contains("not enabled"));
    }
}