using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WFAI.Application.Dtos.JWT;
using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Features.Token.Queries.LoginWith2FA;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Tests.Support;

namespace WFAI.Infrastructure.Tests.Identity.Services;

public class TokenService2FATests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly JwtConfiguration _jwtConfig;
    private readonly TokenService _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private string ChallengeIssuer => $"{_jwtConfig.Issuer}:2fa-challenge";
    private const string ChallengeAudience = "2fa-challenge";
    private const string ChallengeClaim = "2fa_challenge";

    public TokenService2FATests()
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

    private ApplicationUser MakeUser(bool twoFactorEnabled = false, bool active = true, bool confirmed = true) =>
        new()
        {
            Id = 1,
            Email = "user@test.com",
            UserName = "user@test.com",
            FullName = "Test User",
            PhoneNumber = "01012345678",
            IsActive = active,
            EmailConfirmed = confirmed,
            TwoFactorEnabled = twoFactorEnabled,
            RefreshToken = "existing-refresh",
            RefreshTokenExpiryDate = FixedNow.AddDays(3)
        };

    private string BuildChallengeToken(int userId, string jti, DateTime? expiry = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ChallengeClaim, "true"),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };
        var token = new JwtSecurityToken(
            issuer: ChallengeIssuer,
            audience: ChallengeAudience,
            claims: claims,
            expires: expiry ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetupCacheNotFound()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((byte[]?)null);
    }

    private void SetupCacheFound(string key)
    {
        _cache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new byte[] { 1 });
    }

    private void SetupCacheSet()
    {
        _cache.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // â”€â”€ GetTokenAsync 2FA branch â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetTokenAsync_TwoFactorEnabled_ReturnsRequiresTwoFactorTrue()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Pass@1" });

        result.IsSuccessful.Should().BeTrue();
        result.Data!.RequiresTwoFactor.Should().BeTrue();
        result.Data.TwoFactorChallengeToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTokenAsync_TwoFactorEnabled_ReturnsChallengeTokenWithCorrectIssuerAndAudience()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Pass@1" });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Data!.TwoFactorChallengeToken);
        jwt.Issuer.Should().Be(ChallengeIssuer);
        jwt.Audiences.Should().Contain(ChallengeAudience);
        jwt.Claims.Should().Contain(c => c.Type == ChallengeClaim && c.Value == "true");
    }

    [Fact]
    public async Task GetTokenAsync_TwoFactorEnabled_ChallengeTokenExpiresAfterConfiguredMinutes()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Pass@1" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Data!.TwoFactorChallengeToken);
        jwt.ValidTo.Should().BeCloseTo(
            FixedNow.AddMinutes(_jwtConfig.TwoFactorChallengeTokenExpiryInMinutes),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetTokenAsync_TwoFactorEnabled_DoesNotCallResetAccessFailedCount()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Pass@1" });

        _userManager.Verify(m => m.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_TwoFactorDisabled_ReturnsRealTokensWithRequiresTwoFactorFalse()
    {
        var user = MakeUser(twoFactorEnabled: false);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@1")).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.GetTokenAsync(new TokenRequest { Email = user.Email!, Password = "Pass@1" });

        result.IsSuccessful.Should().BeTrue();
        result.Data!.RequiresTwoFactor.Should().BeFalse();
        result.Data.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.TwoFactorChallengeToken.Should().BeNull();
    }

    // â”€â”€ LoginWith2FAAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task LoginWith2FAAsync_InvalidChallengeTokenSignature_ReturnsFail()
    {
        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = "invalid.token.here",
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("Invalid or expired"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_ExpiredChallengeToken_ReturnsFail()
    {
        var expiredToken = BuildChallengeToken(1, Guid.NewGuid().ToString(),
            expiry: DateTime.UtcNow.AddMinutes(-1));

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = expiredToken,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("Invalid or expired"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_MissingChallengeClaim_ReturnsFail()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: ChallengeIssuer,
            audience: ChallengeAudience,
            claims: [new Claim(ClaimTypes.NameIdentifier, "1")],
            expires: FixedNow.AddMinutes(5),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = tokenString,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("Invalid or expired"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_JtiAlreadyInCache_ReturnsFail()
    {
        var jti = Guid.NewGuid().ToString();
        var token = BuildChallengeToken(1, jti);
        SetupCacheFound($"2fa_jti:{jti}");

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("already been used"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_UserLockedOut_ReturnsFail()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("locked"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_TwoFactorNotEnabledOnAccount_ReturnsFail()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: false);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("not enabled"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_BothTOTPAndRecoveryCodeFail_ReturnsFailAndCallsAccessFailed()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "bad-code"))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.RedeemTwoFactorRecoveryCodeAsync(user, "bad-code"))
            .ReturnsAsync(IdentityResult.Failed());
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "bad-code"
        });

        result.IsSuccessful.Should().BeFalse();
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginWith2FAAsync_WrongCodeExceedsThreshold_ReturnsLockedOutMessage()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user))
            .ReturnsAsync(false)     // first check (before code verification)
            .Callback(() => _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true));
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.RedeemTwoFactorRecoveryCodeAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed());
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "bad-code"
        });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("locked"));
    }

    [Fact]
    public async Task LoginWith2FAAsync_ValidTOTPCode_ReturnsRealTokens()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        SetupCacheSet();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshTokenExpiryTime.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginWith2FAAsync_ValidTOTPCode_StoresJtiInCacheWithCorrectTTL()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        DistributedCacheEntryOptions? capturedOptions = null;
        _cache.Setup(c => c.SetAsync(
                $"2fa_jti:{jti}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts)
            .Returns(Task.CompletedTask);
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        _cache.Verify(c => c.SetAsync(
            $"2fa_jti:{jti}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(
            TimeSpan.FromMinutes(_jwtConfig.TwoFactorChallengeTokenExpiryInMinutes));
    }

    [Fact]
    public async Task LoginWith2FAAsync_ValidTOTPCode_CallsResetAccessFailedCount()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        SetupCacheSet();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "123456"
        });

        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginWith2FAAsync_ValidRecoveryCode_ReturnsRealTokens()
    {
        var jti = Guid.NewGuid().ToString();
        var user = MakeUser(twoFactorEnabled: true);
        var token = BuildChallengeToken(user.Id, jti);
        SetupCacheNotFound();
        SetupCacheSet();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "recovery-code-1"))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.RedeemTwoFactorRecoveryCodeAsync(user, "recovery-code-1"))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.LoginWith2FAAsync(new TwoFactorLoginRequest
        {
            TwoFactorChallengeToken = token,
            Code = "recovery-code-1"
        });

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }
}