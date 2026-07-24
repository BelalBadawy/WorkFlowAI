using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class LoginWith2FAEndpointTests : ApiTestBase
{
    public LoginWith2FAEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LoginWith2FA_EmptyFields_ReturnsFailure()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/login-2fa", new
        {
            TwoFactorChallengeToken = "",
            Code = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(m => !string.IsNullOrWhiteSpace(m));
    }

    [Fact]
    public async Task LoginWith2FA_InvalidChallengeToken_ReturnsFailure()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/login-2fa", new
        {
            TwoFactorChallengeToken = "not.a.valid.jwt.token",
            Code = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }
}