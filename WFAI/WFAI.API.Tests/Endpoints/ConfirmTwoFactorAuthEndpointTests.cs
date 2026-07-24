using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class ConfirmTwoFactorAuthEndpointTests : ApiTestBase
{
    public ConfirmTwoFactorAuthEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConfirmTwoFactorAuth_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/users/confirm-2fa", new { Code = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmTwoFactorAuth_NonNumericCode_ReturnsValidationError()
    {
        var email = $"confirm-2fa-val-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PostAsJsonAsync("/api/v1/users/confirm-2fa", new { Code = "abcdef" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(m => !string.IsNullOrWhiteSpace(m));
    }

    [Fact]
    public async Task ConfirmTwoFactorAuth_WrongCode_WhenNotSetUp_ReturnsFailure()
    {
        var email = $"confirm-2fa-wrong-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);
        UseSelfServiceClient(user.Id);

        var response = await Client.PostAsJsonAsync("/api/v1/users/confirm-2fa", new { Code = "000000" });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }
}