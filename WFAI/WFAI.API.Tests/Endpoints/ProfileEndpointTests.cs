using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class ProfileEndpointTests : ApiTestBase
{
    public ProfileEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Profile_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/account/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_Authenticated_ReturnsProfileData()
    {
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.GetAsync("/api/v1/account/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<ProfileResponseContract>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(user.Id);
        payload.Data.Email.Should().Be(email);
        payload.Data.TwoFactorEnabled.Should().BeFalse();
    }
}