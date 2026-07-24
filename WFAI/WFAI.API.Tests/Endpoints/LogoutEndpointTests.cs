using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class LogoutEndpointTests : ApiTestBase
{
    public LogoutEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/logout", new
        {
            RefreshToken = "any-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Authenticated_ValidRefreshToken_ReturnsSuccess()
    {
        var email = $"logout-valid-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PostAsJsonAsync("/api/v1/account/logout", new
        {
            RefreshToken = user.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_Authenticated_InvalidRefreshToken_ReturnsFailure()
    {
        var email = $"logout-invalid-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PostAsJsonAsync("/api/v1/account/logout", new
        {
            RefreshToken = "this-token-does-not-match"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }
}