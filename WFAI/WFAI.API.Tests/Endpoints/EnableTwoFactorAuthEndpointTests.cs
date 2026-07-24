using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class EnableTwoFactorAuthEndpointTests : ApiTestBase
{
    public EnableTwoFactorAuthEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task EnableTwoFactorAuth_Unauthenticated_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/users/enable-2fa", new { Code = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnableTwoFactorAuth_InvalidCodeFormat_ReturnsValidationError()
    {
        var email = $"enable-2fa-val-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PutAsJsonAsync("/api/v1/users/enable-2fa", new { Code = "abc" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(m => !string.IsNullOrWhiteSpace(m));
    }

    [Fact]
    public async Task EnableTwoFactorAuth_WrongCode_ReturnsFailure()
    {
        var email = $"enable-2fa-wrong-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PutAsJsonAsync("/api/v1/users/enable-2fa", new { Code = "000000" });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }
}