using System.Net;
using System.Net.Http.Json;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class AccountEndpointsTests : ApiTestBase
{
    public AccountEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_should_return_token_for_seeded_user()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        const string password = "Admin@123";
        await Seeder.SeedUserAsync(email, password, ["Basic"]);

        var response = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_should_return_unsuccessful_payload_when_credentials_are_invalid()
    {
        var email = $"invalid-login-{Guid.NewGuid():N}@example.com";
        await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);

        var response = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = "WrongPassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task Forgot_password_should_return_successful_response_and_capture_reset_email()
    {
        var email = $"forgot-{Guid.NewGuid():N}@example.com";
        await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);

        var emailSink = GetRequiredService<ApiTestEmailSink>();
        emailSink.Clear();

        var response = await Client.PostAsJsonAsync($"/api/v1/account/forgot-password?email={email}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        emailSink.FindLatestFor(email).Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ReturnsNewToken()
    {
        // Arrange
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        const string password = "Admin@123";
        await Seeder.SeedUserAsync(email, password, ["Basic"]);

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = password
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/account/refresh-token", new
        {
            Token = loginPayload!.Data!.Token,
            RefreshToken = loginPayload.Data.RefreshToken
        });

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.Data.RefreshToken.Should().NotBe(loginPayload.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidPayload_ReturnsUnsuccessfulPayload()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/refresh-token", new
        {
            Token = "",
            RefreshToken = ""
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(message => !string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task ResetPassword_UnknownEmail_ReturnsUnsuccessfulPayload()
    {
        // Arrange
        var request = new
        {
            Token = "invalid-token",
            Email = "missing@example.com",
            Password = "NewPassword@123",
            ConfirmPassword = "NewPassword@123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/account/reset-password", request);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
        payload.Messages.Should().Contain(message => !string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ResetsPasswordAndAllowsLogin()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Admin@123";
        const string newPassword = "NewPassword@123";
        await Seeder.SeedUserAsync(email, oldPassword, ["Basic"]);

        var emailSink = GetRequiredService<ApiTestEmailSink>();
        emailSink.Clear();

        var forgotPasswordResponse = await Client.PostAsJsonAsync($"/api/v1/account/forgot-password?email={email}", new { });
        forgotPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = emailSink.GetLatestResetToken(email);

        var response = await Client.PostAsJsonAsync("/api/v1/account/reset-password", new
        {
            Token = token,
            Email = email,
            Password = newPassword,
            ConfirmPassword = newPassword
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = newPassword
        });
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ResponseContract<TokenResponseContract>>();

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginPayload.Should().NotBeNull();
        loginPayload!.IsSuccessful.Should().BeTrue();
        loginPayload.Data.Should().NotBeNull();
        loginPayload.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmEmail_UnknownUser_ReturnsUnsuccessfulPayload()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/confirm-email", new
        {
            UserId = 999999,
            Token = "irrelevant-token"
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_ConfirmsEmailSuccessfully()
    {
        var email = $"confirm-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUnconfirmedUserAsync(email, "Admin@123");

        var emailSink = GetRequiredService<ApiTestEmailSink>();
        emailSink.Clear();

        var resendResponse = await Client.PostAsJsonAsync("/api/v1/account/resend-confirmation-email", new
        {
            Email = email
        });
        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = emailSink.GetQueryParam(email, "token");
        var userId = int.Parse(emailSink.GetQueryParam(email, "userId"));

        var response = await Client.PostAsJsonAsync("/api/v1/account/confirm-email", new
        {
            UserId = userId,
            Token = token
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailChange_UnknownUser_ReturnsUnsuccessfulPayload()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/confirm-email-change", new
        {
            UserId = 999999,
            NewEmail = "changed@example.com",
            Token = "irrelevant-token"
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmailChange_InvalidToken_ReturnsUnsuccessfulPayload()
    {
        var email = $"change-email-{Guid.NewGuid():N}@example.com";
        var user = await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);

        var response = await Client.PostAsJsonAsync("/api/v1/account/confirm-email-change", new
        {
            UserId = user.Id,
            NewEmail = "newaddr@example.com",
            Token = "bad-token"
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task ResendConfirmationEmail_UnknownEmail_ReturnsSafeSuccessResponse()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/account/resend-confirmation-email", new
        {
            Email = $"ghost-{Guid.NewGuid():N}@example.com"
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task ResendConfirmationEmail_AlreadyConfirmedEmail_ReturnsSafeSuccessResponse()
    {
        var email = $"confirmed-resend-{Guid.NewGuid():N}@example.com";
        await Seeder.SeedUserAsync(email, "Admin@123", ["Basic"]);

        var response = await Client.PostAsJsonAsync("/api/v1/account/resend-confirmation-email", new
        {
            Email = email
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task ResendConfirmationEmail_UnconfirmedEmail_SendsEmailAndReturnsSuccess()
    {
        var email = $"unconfirmed-resend-{Guid.NewGuid():N}@example.com";
        await Seeder.SeedUnconfirmedUserAsync(email, "Admin@123");

        var emailSink = GetRequiredService<ApiTestEmailSink>();
        emailSink.Clear();

        var response = await Client.PostAsJsonAsync("/api/v1/account/resend-confirmation-email", new
        {
            Email = email
        });
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        emailSink.FindLatestFor(email).Should().NotBeNull();
    }
}