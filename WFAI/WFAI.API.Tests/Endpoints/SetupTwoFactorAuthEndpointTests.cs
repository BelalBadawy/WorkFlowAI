using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class SetupTwoFactorAuthEndpointTests : ApiTestBase
{
    public SetupTwoFactorAuthEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SetupTwoFactorAuth_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsync("/api/v1/users/setup-2fa", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetupTwoFactorAuth_Authenticated_ReturnsSetupData()
    {
        var email = $"setup-2fa-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PostAsync("/api/v1/users/setup-2fa", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<TwoFactorSetupResponseContract>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.KeySecret.Should().NotBeNullOrWhiteSpace();
        payload.Data.CodeQR.Should().NotBeNullOrWhiteSpace();
    }
}