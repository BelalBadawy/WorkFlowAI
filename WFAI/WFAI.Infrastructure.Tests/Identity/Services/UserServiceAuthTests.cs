using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Dtos.TwoFactor;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth;
using WFAI.Application.Features.Users.Commands.Logout;
using WFAI.Application.Features.Users.Models.Requests;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Tests.Support;

namespace WFAI.Infrastructure.Tests.Identity.Services;

public class UserServiceAuthTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly UserService _sut;

    private const int TestUserId = 42;
    private static readonly DateTime FixedNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public UserServiceAuthTests()
    {
        _userManager = IdentityMockFactory.CreateUserManager();
        _roleManager = IdentityMockFactory.CreateRoleManager();
        _dateTimeService.Setup(d => d.NowUtc).Returns(FixedNow);
        _currentUserService.Setup(s => s.GetUserId()).Returns(TestUserId);

        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("example.com"));
        mockRequest.Setup(r => r.PathBase).Returns(new PathString(""));
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var twoFactorOptions = Options.Create(new TwoFactorOptions { Issuer = "TestApp" });
        var clientSettings = Options.Create(new ClientSettings { BaseUrl = "http://client.example.com" });

        _sut = new UserService(
            _userManager.Object,
            _roleManager.Object,
            _emailService.Object,
            _httpContextAccessor.Object,
            _dateTimeService.Object,
            _currentUserService.Object,
            twoFactorOptions,
            clientSettings,
            new Mock<ILogger<UserService>>().Object,
            _dbContext.Object);
    }

    private ApplicationUser MakeUser(bool twoFactorEnabled = false) =>
        new()
        {
            Id = TestUserId,
            Email = "user@test.com",
            UserName = "user@test.com",
            FullName = "Test User",
            IsActive = true,
            EmailConfirmed = true,
            TwoFactorEnabled = twoFactorEnabled,
            PhoneNumber = "01012345678",
            CreatedDate = FixedNow.AddDays(-30),
            RefreshToken = "stored-refresh-token",
            RefreshTokenExpiryDate = FixedNow.AddDays(1)
        };

    // â”€â”€ LogoutAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task LogoutAsync_EmptyRefreshToken_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(MakeUser());

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "" });

        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_RefreshTokenDoesNotMatchStored_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(MakeUser());

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "wrong-token" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("Invalid refresh token"));
    }

    [Fact]
    public async Task LogoutAsync_ExpiredButMatchingToken_ClearsTokenAndReturnsSuccess()
    {
        var user = MakeUser();
        user.RefreshTokenExpiryDate = FixedNow.AddDays(-1);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "stored-refresh-token" });

        result.IsSuccessful.Should().BeTrue();
        user.RefreshToken.Should().BeEmpty();
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_ClearsRefreshTokenAndSetsExpiryToThePast()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "stored-refresh-token" });

        result.IsSuccessful.Should().BeTrue();
        user.RefreshToken.Should().BeEmpty();
        user.RefreshTokenExpiryDate.Should().BeBefore(FixedNow);
    }

    // â”€â”€ GetMyProfileAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetMyProfileAsync_ReturnsProfileWithCorrectUserFields()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Basic"]);
        var basicRole = new ApplicationRole { Name = "Basic" };
        _roleManager.Setup(r => r.FindByNameAsync("Basic")).ReturnsAsync(basicRole);
        _roleManager.Setup(r => r.GetClaimsAsync(basicRole)).ReturnsAsync([]);

        var result = await _sut.GetMyProfileAsync();

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Id.Should().Be(user.Id);
        result.Data.Email.Should().Be(user.Email);
        result.Data.FullName.Should().Be(user.FullName);
        result.Data.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyProfileAsync_ReturnsDeduplicatedPermissionsFromAllRoles()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Basic", "Admin"]);
        var basicRole = new ApplicationRole { Name = "Basic" };
        var adminRole = new ApplicationRole { Name = "Admin" };
        _roleManager.Setup(r => r.FindByNameAsync("Basic")).ReturnsAsync(basicRole);
        _roleManager.Setup(r => r.FindByNameAsync("Admin")).ReturnsAsync(adminRole);
        _roleManager.Setup(r => r.GetClaimsAsync(basicRole))
            .ReturnsAsync([new System.Security.Claims.Claim("permission", "perm.read")]);
        _roleManager.Setup(r => r.GetClaimsAsync(adminRole))
            .ReturnsAsync([
                new System.Security.Claims.Claim("permission", "perm.read"),
                new System.Security.Claims.Claim("permission", "perm.write")
            ]);

        var result = await _sut.GetMyProfileAsync();

        result.Data!.Permissions.Should().HaveCount(2);
        result.Data.Permissions.Should().Contain("perm.read");
        result.Data.Permissions.Should().Contain("perm.write");
    }

    [Fact]
    public async Task GetMyProfileAsync_ReturnsFlatPermissionClaimValues()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Basic"]);
        var basicRole = new ApplicationRole { Name = "Basic" };
        _roleManager.Setup(r => r.FindByNameAsync("Basic")).ReturnsAsync(basicRole);
        _roleManager.Setup(r => r.GetClaimsAsync(basicRole))
            .ReturnsAsync([new System.Security.Claims.Claim("permission", "Identity.Users.Read")]);

        var result = await _sut.GetMyProfileAsync();

        result.Data!.Permissions.Should().ContainSingle().Which.Should().Be("Identity.Users.Read");
    }

    // â”€â”€ SetupTwoFactorAuthAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task SetupTwoFactorAuthAsync_TwoFactorAlreadyEnabled_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(MakeUser(twoFactorEnabled: true));

        var result = await _sut.SetupTwoFactorAuthAsync();

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("already enabled"));
    }

    [Fact]
    public async Task SetupTwoFactorAuthAsync_ExistingKeyPresent_ReturnsExistingKeyWithoutCallingReset()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("EXISTING-KEY");

        var result = await _sut.SetupTwoFactorAuthAsync();

        result.IsSuccessful.Should().BeTrue();
        result.Data!.KeySecret.Should().Be("EXISTING-KEY");
        _userManager.Verify(m => m.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SetupTwoFactorAuthAsync_NoKeyPresent_GeneratesAndReturnsNewKey()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.SetupSequence(m => m.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync((string?)null)
            .ReturnsAsync("NEW-GENERATED-KEY");
        _userManager.Setup(m => m.ResetAuthenticatorKeyAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.SetupTwoFactorAuthAsync();

        result.IsSuccessful.Should().BeTrue();
        result.Data!.KeySecret.Should().Be("NEW-GENERATED-KEY");
        _userManager.Verify(m => m.ResetAuthenticatorKeyAsync(user), Times.Once);
    }

    [Fact]
    public async Task SetupTwoFactorAuthAsync_ReturnedCodeQRIsValidOtpauthUri()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("JBSWY3DPEHPK3PXP");

        var result = await _sut.SetupTwoFactorAuthAsync();

        result.Data!.CodeQR.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public async Task SetupTwoFactorAuthAsync_OtpauthUriContainsEncodedIssuerAndEmail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("JBSWY3DPEHPK3PXP");

        var result = await _sut.SetupTwoFactorAuthAsync();

        result.Data!.CodeQR.Should().Contain("TestApp");
        result.Data.CodeQR.Should().Contain("user%40test.com");
        result.Data.CodeQR.Should().Contain("JBSWY3DPEHPK3PXP");
    }

    // â”€â”€ ConfirmTwoFactorAuthAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ConfirmTwoFactorAuthAsync_NoAuthenticatorKeyExists_ReturnsFail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync((string?)null);

        var result = await _sut.ConfirmTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("setup-2fa"));
    }

    [Fact]
    public async Task ConfirmTwoFactorAuthAsync_WrongCode_ReturnsFailAndCallsAccessFailed()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "000000"))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "000000" });

        result.IsSuccessful.Should().BeFalse();
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task ConfirmTwoFactorAuthAsync_ValidCode_ReturnsSuccessAndCallsResetAccessFailed()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeTrue();
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ConfirmTwoFactorAuthAsync_WorksWhenTwoFactorIsAlreadyEnabled()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeTrue();
    }

    // â”€â”€ EnableTwoFactorAuthAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task EnableTwoFactorAuthAsync_AlreadyEnabled_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(MakeUser(twoFactorEnabled: true));

        var result = await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("already enabled"));
    }

    [Fact]
    public async Task EnableTwoFactorAuthAsync_NoAuthenticatorKey_ReturnsFail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync((string?)null);

        var result = await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("setup-2fa"));
    }

    [Fact]
    public async Task EnableTwoFactorAuthAsync_WrongCode_ReturnsFailAndCallsAccessFailed()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "000000"))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "000000" });

        result.IsSuccessful.Should().BeFalse();
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task EnableTwoFactorAuthAsync_WrongCodeExceedsThreshold_ReturnsLockedOutMessage()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "000000" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("locked"));
    }

    [Fact]
    public async Task EnableTwoFactorAuthAsync_ValidCode_SetsTwoFactorEnabledTrue()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync(Enumerable.Range(1, 10).Select(i => $"code-{i}").ToArray());

        await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        _userManager.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Once);
    }

    [Fact]
    public async Task EnableTwoFactorAuthAsync_ValidCode_ReturnsTenRecoveryCodes()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("KEY");
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync(Enumerable.Range(1, 10).Select(i => $"code-{i}").ToArray());

        var result = await _sut.EnableTwoFactorAuthAsync(new TwoFactorCodeRequest { Code = "123456" });

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(10);
    }

    // â”€â”€ DisableTwoFactorAuthAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task DisableTwoFactorAuthAsync_TwoFactorNotEnabled_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(MakeUser(twoFactorEnabled: false));

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest { Password = "Pass@123" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("not enabled"));
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_WrongPassword_ReturnsFailAndCallsAccessFailed()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "WrongPass")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest { Password = "WrongPass" });

        result.IsSuccessful.Should().BeFalse();
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_WrongPasswordExceedsThreshold_ReturnsLockedOutMessage()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "WrongPass")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest { Password = "WrongPass" });

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("locked"));
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_WrongTOTPCode_ReturnsFailWithoutCallingAccessFailed()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "000000"))
            .ReturnsAsync(false);

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest
        {
            Password = "Pass@123",
            Code = "000000"
        });

        result.IsSuccessful.Should().BeFalse();
        _userManager.Verify(m => m.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_ValidPasswordNoCode_SetsTwoFactorEnabledFalse()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(user, false)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest
        {
            Password = "Pass@123",
            Code = null
        });

        result.IsSuccessful.Should().BeTrue();
        _userManager.Verify(m => m.SetTwoFactorEnabledAsync(user, false), Times.Once);
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_ValidPasswordValidCode_SetsTwoFactorEnabledFalse()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(user, false)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest
        {
            Password = "Pass@123",
            Code = "123456"
        });

        result.IsSuccessful.Should().BeTrue();
        _userManager.Verify(m => m.SetTwoFactorEnabledAsync(user, false), Times.Once);
    }

    [Fact]
    public async Task DisableTwoFactorAuthAsync_DoesNotCallResetAuthenticatorKey()
    {
        var user = MakeUser(twoFactorEnabled: true);
        _userManager.Setup(m => m.FindByIdAsync(TestUserId.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(user, false)).ReturnsAsync(IdentityResult.Success);

        await _sut.DisableTwoFactorAuthAsync(new DisableTwoFactorAuthRequest { Password = "Pass@123" });

        _userManager.Verify(m => m.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}