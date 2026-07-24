using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WFAI.Application.Dtos.JWT;
using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Tests.Support;

namespace WFAI.Infrastructure.Tests.Identity.Services;

public class TokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly JwtConfiguration _jwtConfig;
    private readonly TokenService _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public TokenServiceTests()
    {
        _userManager = IdentityMockFactory.CreateUserManager();
        _roleManager = IdentityMockFactory.CreateRoleManager();
        _dateTimeService.Setup(d => d.NowUtc).Returns(FixedNow);

        _jwtConfig = new JwtConfiguration
        {
            Issuer = "ums-issuer",
            Audience = "ums-audience",
            Secret = "super-secret-key-for-testing-123456",
            TokenExpiryInMinutes = 60,
            RefreshTokenExpiryInDays = 7,
            TwoFactorChallengeTokenExpiryInMinutes = 5
        };

        _sut = new TokenService(
            _userManager.Object,
            _roleManager.Object,
            Options.Create(_jwtConfig),
            _dateTimeService.Object,
            _cache.Object);
    }

    private ApplicationUser MakeUser(string email = "user@test.com", bool active = true, bool confirmed = true, bool lockedOut = false) =>
        new()
        {
            Id = 1,
            Email = email,
            UserName = email,
            FullName = "Test User",
            PhoneNumber = "01012345678",
            IsActive = active,
            EmailConfirmed = confirmed,
            RefreshToken = "existing-refresh",
            RefreshTokenExpiryDate = FixedNow.AddDays(3)
        };

    private string BuildJwt(string email, DateTime? expiry = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.Email, email) };
        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: expiry ?? FixedNow.AddMinutes(60),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // â”€â”€ GetTokenAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetTokenAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByEmailAsync("x@x.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = "x@x.com", Password = "p" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid Credentials.");
    }

    [Fact]
    public async Task GetTokenAsync_WhenUserInactive_ReturnsFail()
    {
        var user = MakeUser(active: false);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "p" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Contain("not active");
    }

    [Fact]
    public async Task GetTokenAsync_WhenEmailNotConfirmed_ReturnsFail()
    {
        var user = MakeUser(confirmed: false);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "p" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email not confirmed.");
    }

    [Fact]
    public async Task GetTokenAsync_WhenPasswordInvalid_ReturnsFail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "wrong" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid Credentials.");
    }

    [Fact]
    public async Task GetTokenAsync_WhenLockedOut_ReturnsFail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "pass" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Contain("locked");
    }

    [Fact]
    public async Task GetTokenAsync_WhenSuccessful_ReturnsTokenResponseWithRequiredClaims()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Valid@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        var adminRole = new ApplicationRole { Id = 1, Name = "Admin" };
        _roleManager.Setup(m => m.FindByNameAsync("Admin")).ReturnsAsync(adminRole);
        _roleManager.Setup(m => m.GetClaimsAsync(adminRole)).ReturnsAsync([new Claim("permission", "Permission.Identity.Users.Read")]);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Valid@1" });

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshTokenExpiryTime.Should().BeAfter(FixedNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Data.Token);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Value == "Permission.Identity.Users.Read");
        jwt.ValidTo.Should().BeCloseTo(FixedNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetTokenAsync_WhenSuccessful_RotatesRefreshToken()
    {
        var user = MakeUser();
        var originalRefresh = user.RefreshToken;
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Valid@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Valid@1" });

        result.Data!.RefreshToken.Should().NotBe(originalRefresh);
    }

    // â”€â”€ GetRefreshTokenAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetRefreshTokenAsync_WhenUserNotFound_ReturnsFail()
    {
        var jwt = BuildJwt("ghost@test.com");
        _userManager.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetRefreshTokenAsync(new RefreshTokenRequest { Token = jwt, RefreshToken = "any" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenRefreshTokenMismatch_ReturnsFail()
    {
        var user = MakeUser();
        user.RefreshToken = "correct-refresh";
        var jwt = BuildJwt(user.Email!);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var result = await _sut.GetRefreshTokenAsync(new RefreshTokenRequest { Token = jwt, RefreshToken = "wrong-refresh" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid token provided.");
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenRefreshTokenExpired_ReturnsFail()
    {
        var user = MakeUser();
        user.RefreshToken = "valid-token";
        user.RefreshTokenExpiryDate = FixedNow.AddDays(-1);
        var jwt = BuildJwt(user.Email!);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var result = await _sut.GetRefreshTokenAsync(new RefreshTokenRequest { Token = jwt, RefreshToken = "valid-token" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid token provided.");
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenValid_ReturnsNewTokenAndRotatesRefresh()
    {
        var user = MakeUser();
        user.RefreshToken = "correct-refresh";
        var jwt = BuildJwt(user.Email!);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.GetRefreshTokenAsync(new RefreshTokenRequest { Token = jwt, RefreshToken = "correct-refresh" });

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBe("correct-refresh");
        result.Data.RefreshTokenExpiryTime.Should().BeAfter(FixedNow);
    }
}