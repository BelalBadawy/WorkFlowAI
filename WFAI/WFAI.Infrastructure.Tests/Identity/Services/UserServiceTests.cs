using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Dtos.Email;
using WFAI.Application.Dtos.TwoFactor;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Tests.Support;

namespace WFAI.Infrastructure.Tests.Identity.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly UserService _sut;

    private static readonly DateTime FixedNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public UserServiceTests()
    {
        _userManager = IdentityMockFactory.CreateUserManager();
        _roleManager = IdentityMockFactory.CreateRoleManager();

        _dateTimeService.Setup(d => d.NowUtc).Returns(FixedNow);

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

    private static ApplicationUser MakeUser(int id = 1, string email = "user@test.com", bool confirmed = true, bool active = true) =>
        new()
        {
            Id = id,
            Email = email,
            UserName = email,
            FullName = "Test User",
            IsActive = active,
            EmailConfirmed = confirmed,
            RefreshToken = "token",
            RefreshTokenExpiryDate = FixedNow.AddDays(1)
        };

    // â”€â”€ RegisterUserAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RegisterUserAsync_WhenEmailAlreadyTaken_ReturnsFail()
    {
        var req = new UserRegistrationRequest { Email = "taken@test.com", Password = "Pass@1", FullName = "X", AutoConfirmEmail = true, ActivateUser = true };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(MakeUser(email: req.Email));

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email address already taken.");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenUserCreationFails_ReturnsFail()
    {
        var req = new UserRegistrationRequest { Email = "new@test.com", Password = "Pass@1", FullName = "X", AutoConfirmEmail = true, ActivateUser = true };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Password too weak.");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenRoleAssignmentFails_ReturnsFail()
    {
        var req = new UserRegistrationRequest { Email = "new@test.com", Password = "Pass@1", FullName = "X", AutoConfirmEmail = true, ActivateUser = true };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Basic"))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found." }));

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role not found.");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenSuccessful_ReturnsSuccess()
    {
        var req = new UserRegistrationRequest { Email = "new@test.com", Password = "Pass@1", FullName = "Alice", AutoConfirmEmail = true, ActivateUser = true };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Basic"))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User registered successfully.");
    }

    // â”€â”€ UpdateUserAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UpdateUserAsync_WhenUserNotFound_ReturnsFail()
    {
        var req = new UpdateUserRequest { UserId = 99, FullName = "X", PhoneNumber = "000" };
        _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.UpdateUserAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exists.");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUpdateFails_ReturnsFail()
    {
        var user = MakeUser(5);
        var req = new UpdateUserRequest { UserId = 5, FullName = "New Name", PhoneNumber = "111" };
        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency conflict." }));

        var result = await _sut.UpdateUserAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Concurrency conflict.");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenSuccessful_ReturnsSuccess()
    {
        var user = MakeUser(5);
        var req = new UpdateUserRequest { UserId = 5, FullName = "New Name", PhoneNumber = "111" };
        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateUserAsync(req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User updated successfully.");
    }

    // â”€â”€ GetUserByIdAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("77")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetUserByIdAsync(77);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exists.");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserFound_ReturnsMappedUserResponse()
    {
        var user = MakeUser(3, "bob@test.com");
        _userManager.Setup(m => m.FindByIdAsync("3")).ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync(3);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(3);
        result.Data.Email.Should().Be("bob@test.com");
    }

    // â”€â”€ GetUsersPagedQueryAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetUsersPagedQueryAsync_ReturnsPaginatedResult()
    {
        var users = Enumerable.Range(1, 10)
            .Select(i => MakeUser(i, $"user{i}@test.com"))
            .ToList();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>(users));

        var request = new WFAI.Application.Dtos.Pagination.PagedFilterRequest
        {
            PageNumber = 1,
            PageSize = 5,
            SortBy = "email",
            SortDirection = "asc"
        };

        var result = await _sut.GetUsersPagedQueryAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(10);
        result.Data.Data.Should().HaveCount(5);
        result.Data.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task GetUsersPagedQueryAsync_SortByIdDesc_OrdersCorrectly()
    {
        var users = new List<ApplicationUser> { MakeUser(1), MakeUser(3), MakeUser(2) };
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>(users));

        var request = new WFAI.Application.Dtos.Pagination.PagedFilterRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "id",
            SortDirection = "desc"
        };

        var result = await _sut.GetUsersPagedQueryAsync(request, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.Data.Select(u => u.Id).Should().BeInDescendingOrder();
    }

    // â”€â”€ ChangeUserPasswordAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ChangeUserPasswordAsync_WhenUserNotFound_ReturnsFail()
    {
        var req = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" };
        _userManager.Setup(m => m.FindByIdAsync("10")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ChangeUserPasswordAsync(10, req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task ChangeUserPasswordAsync_WhenPasswordChangeFails_ReturnsFail()
    {
        var user = MakeUser(10);
        var req = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" };
        _userManager.Setup(m => m.FindByIdAsync("10")).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangePasswordAsync(user, "old", "new"))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect current password." }));

        var result = await _sut.ChangeUserPasswordAsync(10, req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Incorrect current password.");
    }

    [Fact]
    public async Task ChangeUserPasswordAsync_WhenSuccessful_ReturnsSuccess()
    {
        var user = MakeUser(10);
        var req = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "New@123" };
        _userManager.Setup(m => m.FindByIdAsync("10")).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangePasswordAsync(user, "old", "New@123"))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ChangeUserPasswordAsync(10, req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User password updated.");
    }

    // â”€â”€ ChangeUserStatusAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ChangeUserStatusAsync_WhenUserNotFound_ReturnsFail()
    {
        var req = new ChangeUserStatusRequest { UserId = 20, ActivateOrDeactivate = true };
        _userManager.Setup(m => m.FindByIdAsync("20")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ChangeUserStatusAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenUpdateFails_ReturnsFail()
    {
        var user = MakeUser(20);
        var req = new ChangeUserStatusRequest { UserId = 20, ActivateOrDeactivate = true };
        _userManager.Setup(m => m.FindByIdAsync("20")).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Store error." }));

        var result = await _sut.ChangeUserStatusAsync(req);

        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenActivating_ReturnsActivatedMessage()
    {
        var user = MakeUser(20, active: false);
        var req = new ChangeUserStatusRequest { UserId = 20, ActivateOrDeactivate = true };
        _userManager.Setup(m => m.FindByIdAsync("20")).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ChangeUserStatusAsync(req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User activated successfully.");
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenDeactivating_ReturnsDeactivatedMessage()
    {
        var user = MakeUser(20);
        var req = new ChangeUserStatusRequest { UserId = 20, ActivateOrDeactivate = false };
        _userManager.Setup(m => m.FindByIdAsync("20")).ReturnsAsync(user);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ChangeUserStatusAsync(req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User de-activated successfully");
    }

    [Fact]
    public async Task ChangeUserStatusAsync_WhenDeactivatingAdmin_ReturnsFail()
    {
        var user = MakeUser(20);
        var req = new ChangeUserStatusRequest { UserId = 20, ActivateOrDeactivate = false };
        _userManager.Setup(m => m.FindByIdAsync("20")).ReturnsAsync(user);
        _userManager.Setup(m => m.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

        var result = await _sut.ChangeUserStatusAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot de-activate the system administrator.");
    }

    // â”€â”€ GetUserRolesAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetUserRolesAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetUserRolesAsync(5);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task GetUserRolesAsync_WhenUserFound_ReturnsRoleViewModels()
    {
        var user = MakeUser(5);
        var adminRole = new ApplicationRole { Name = "Admin", Description = "Admins" };
        var basicRole = new ApplicationRole { Name = "Basic", Description = "Users" };
        var roles = new List<ApplicationRole> { adminRole, basicRole };

        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        _roleManager.Setup(m => m.Roles)
                    .Returns(new TestAsyncEnumerable<ApplicationRole>(roles));
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await _sut.GetUserRolesAsync(5);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().ContainSingle().Which.RoleName.Should().Be("Admin");
    }

    // â”€â”€ UpdateUserRolesAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UpdateUserRolesAsync_WhenUserNotFound_ReturnsFail()
    {
        var req = new UpdateUserRolesRequest { UserId = 99, Roles = ["Admin"] };
        var empty = new TestAsyncEnumerable<ApplicationUser>(Enumerable.Empty<ApplicationUser>());
        _userManager.Setup(m => m.Users).Returns(empty);

        var result = await _sut.UpdateUserRolesAsync(req, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenAdminEmail_ReturnsForbidden()
    {
        var adminUser = MakeUser(1, "admin@seed.com");
        var req = new UpdateUserRolesRequest { UserId = 1, Roles = ["Basic"] };
        _userManager.Setup(m => m.Users)
                    .Returns(new TestAsyncEnumerable<ApplicationUser>([adminUser]));
        _userManager.Setup(m => m.IsInRoleAsync(adminUser, "Admin")).ReturnsAsync(true);

        var result = await _sut.UpdateUserRolesAsync(req, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User roles update not permitted.");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenRoleDoesNotExist_ReturnsFail()
    {
        var user = MakeUser(2, "user@test.com");
        var req = new UpdateUserRolesRequest { UserId = 2, Roles = ["NonExistentRole"] };
        _userManager.Setup(m => m.Users)
                    .Returns(new TestAsyncEnumerable<ApplicationUser>([user]));
        _roleManager.Setup(m => m.RoleExistsAsync("NonExistentRole")).ReturnsAsync(false);

        var result = await _sut.UpdateUserRolesAsync(req, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Role 'NonExistentRole' does not exist.");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenRemoveFails_ReturnsFail()
    {
        var user = MakeUser(2, "user@test.com");
        var req = new UpdateUserRolesRequest { UserId = 2, Roles = ["Admin"] };
        _userManager.Setup(m => m.Users)
                    .Returns(new TestAsyncEnumerable<ApplicationUser>([user]));
        _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Basic"]);
        _userManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Remove error." }));

        var result = await _sut.UpdateUserRolesAsync(req, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Remove error.");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenSuccessful_ReturnsSuccess()
    {
        var user = MakeUser(2, "user@test.com");
        var req = new UpdateUserRolesRequest { UserId = 2, Roles = ["Admin"] };
        _userManager.Setup(m => m.Users)
                    .Returns(new TestAsyncEnumerable<ApplicationUser>([user]));
        _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Basic"]);
        _userManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateUserRolesAsync(req, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Updated user roles successfully.");
    }

    // â”€â”€ ForgotPasswordAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ReturnsSafeSuccessResponse()
    {
        _userManager.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ForgotPasswordAsync("ghost@test.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailNotConfirmed_ReturnsSafeSuccessResponse()
    {
        var user = MakeUser(1, "u@t.com", confirmed: false);
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);

        var result = await _sut.ForgotPasswordAsync("u@t.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailServiceThrows_StillReturnsSafeSuccessResponse()
    {
        var user = MakeUser(1, "u@t.com");
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _emailService.Setup(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new InvalidOperationException("SMTP error"));

        var result = await _sut.ForgotPasswordAsync("u@t.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailSent_ReturnsSuccess()
    {
        var user = MakeUser(1, "u@t.com");
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _emailService.Setup(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(string.Empty);

        var result = await _sut.ForgotPasswordAsync("u@t.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
        _emailService.Verify(e => e.SendAsync(
            It.Is<WFAI.Application.Dtos.Email.SendEmailDto>(dto => 
                dto.MailTo == "u@t.com" &&
                dto.MessageBody.Contains("http://client.example.com/reset-password?email=u%40t.com&token=reset-token") &&
                dto.MessageBody.Contains("If the link above does not work, copy and paste the following URL into your browser:")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // â”€â”€ ResetPasswordAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ResetPasswordAsync_WhenUserNotFound_ReturnsFail()
    {
        var req = new ResetPasswordRequest { Email = "ghost@test.com", Token = "tok", Password = "New@1" };
        _userManager.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ResetPasswordAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("This email doesn't exist.");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenEmailNotConfirmed_ReturnsFail()
    {
        var user = MakeUser(1, "u@t.com", confirmed: false);
        var req = new ResetPasswordRequest { Email = "u@t.com", Token = "tok", Password = "New@1" };
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);

        var result = await _sut.ResetPasswordAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("This email is not confirmed.");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenResetFails_ReturnsFail()
    {
        var user = MakeUser(1, "u@t.com");
        var req = new ResetPasswordRequest { Email = "u@t.com", Token = "bad-token", Password = "New@1" };
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, "bad-token", "New@1"))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _sut.ResetPasswordAsync(req);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenSuccessful_ReturnsSuccess()
    {
        var user = MakeUser(1, "u@t.com");
        var req = new ResetPasswordRequest { Email = "u@t.com", Token = "valid-token", Password = "New@123" };
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, "valid-token", "New@123"))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ResetPasswordAsync(req);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Your password has changed successfully.");
    }

    // â”€â”€ RegisterUserAsync (email confirmation path) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RegisterUserAsync_WhenAutoConfirmEmailFalse_SendsConfirmationEmail()
    {
        var req = new UserRegistrationRequest
        {
            Email = "new@test.com", Password = "Pass@1", FullName = "Alice",
            AutoConfirmEmail = false, ActivateUser = true
        };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Basic"))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                    .ReturnsAsync("confirm-token");
        _emailService.Setup(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(string.Empty);

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeTrue();
        _emailService.Verify(e => e.SendAsync(
            It.Is<WFAI.Application.Dtos.Email.SendEmailDto>(dto =>
                dto.MailTo == req.Email &&
                dto.Subject == "Confirm Your Email"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenAutoConfirmEmailTrue_DoesNotSendEmail()
    {
        var req = new UserRegistrationRequest
        {
            Email = "new@test.com", Password = "Pass@1", FullName = "Alice",
            AutoConfirmEmail = true, ActivateUser = true
        };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Basic"))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.RegisterUserAsync(req);

        result.IsSuccessful.Should().BeTrue();
        _emailService.Verify(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // â”€â”€ ConfirmEmailAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ConfirmEmailAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ConfirmEmailAsync(99, "tok");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenAlreadyConfirmed_ReturnsSuccessSilently()
    {
        var user = MakeUser(1, "u@t.com", confirmed: true);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);

        var result = await _sut.ConfirmEmailAsync(1, "any-token");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email is already confirmed.");
        _userManager.Verify(m => m.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenTokenInvalid_ReturnsFail()
    {
        var user = MakeUser(1, "u@t.com", confirmed: false);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.ConfirmEmailAsync(user, "bad-token"))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _sut.ConfirmEmailAsync(1, "bad-token");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenSuccessful_ReturnsSuccess()
    {
        var user = MakeUser(1, "u@t.com", confirmed: false);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.ConfirmEmailAsync(user, "valid-token"))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmEmailAsync(1, "valid-token");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email confirmed successfully.");
    }

    // â”€â”€ ConfirmEmailChangeAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ConfirmEmailChangeAsync(99, "new@test.com", "tok");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenTokenInvalid_ReturnsFail()
    {
        var user = MakeUser(1, "old@test.com");
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangeEmailAsync(user, "new@test.com", "bad-token"))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _sut.ConfirmEmailChangeAsync(1, "new@test.com", "bad-token");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenSuccessful_SyncsUserNameAndReturnsSuccess()
    {
        var user = MakeUser(1, "old@test.com");
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.ChangeEmailAsync(user, "new@test.com", "valid-token"))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetUserNameAsync(user, "new@test.com"))
                    .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmEmailChangeAsync(1, "new@test.com", "valid-token");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email changed successfully.");
        _userManager.Verify(m => m.SetUserNameAsync(user, "new@test.com"), Times.Once);
    }

    // â”€â”€ ResendConfirmationEmailAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ResendConfirmationEmailAsync_WhenUserNotFound_ReturnsSafeSuccessResponse()
    {
        _userManager.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ResendConfirmationEmailAsync("ghost@test.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_WhenAlreadyConfirmed_ReturnsSafeSuccessResponse()
    {
        var user = MakeUser(1, "u@t.com", confirmed: true);
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);

        var result = await _sut.ResendConfirmationEmailAsync("u@t.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_WhenUnconfirmed_SendsEmailAndReturnsSuccess()
    {
        var user = MakeUser(1, "u@t.com", confirmed: false);
        _userManager.Setup(m => m.FindByEmailAsync("u@t.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("confirm-token");
        _emailService.Setup(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(string.Empty);

        var result = await _sut.ResendConfirmationEmailAsync("u@t.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("If the email is registered, you will receive an email shortly.");
        _emailService.Verify(e => e.SendAsync(
            It.Is<WFAI.Application.Dtos.Email.SendEmailDto>(dto => dto.MailTo == "u@t.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // â”€â”€ GenerateChangeEmailTokenAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GenerateChangeEmailTokenAsync_WhenUserNotFound_ReturnsFail()
    {
        _currentUserService.Setup(s => s.GetUserId()).Returns(5);
        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GenerateChangeEmailTokenAsync("new@test.com");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task GenerateChangeEmailTokenAsync_WhenSameEmail_ReturnsFail()
    {
        var user = MakeUser(1, "same@test.com");
        _currentUserService.Setup(s => s.GetUserId()).Returns(1);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);

        var result = await _sut.GenerateChangeEmailTokenAsync("same@test.com");

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("New email must be different from your current email.");
    }

    [Fact]
    public async Task GenerateChangeEmailTokenAsync_WhenSuccessful_SendsEmailAndReturnsSuccess()
    {
        var user = MakeUser(1, "old@test.com");
        _currentUserService.Setup(s => s.GetUserId()).Returns(1);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@test.com")).ReturnsAsync("change-token");
        _emailService.Setup(e => e.SendAsync(It.IsAny<WFAI.Application.Dtos.Email.SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(string.Empty);

        var result = await _sut.GenerateChangeEmailTokenAsync("new@test.com");

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Email change confirmation sent. Please check your inbox.");
        _emailService.Verify(e => e.SendAsync(
            It.Is<WFAI.Application.Dtos.Email.SendEmailDto>(dto =>
                dto.MailTo == "old@test.com" &&
                dto.Subject == "Confirm Your Email Change"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // â”€â”€ GenerateNew2FARecoveryCodesAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GenerateNew2FARecoveryCodesAsync_WhenUserNotFound_ReturnsFail()
    {
        _currentUserService.Setup(s => s.GetUserId()).Returns(5);
        _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GenerateNew2FARecoveryCodesAsync();

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task GenerateNew2FARecoveryCodesAsync_WhenTwoFactorNotEnabled_ReturnsFail()
    {
        var user = MakeUser(1);
        user.TwoFactorEnabled = false;
        _currentUserService.Setup(s => s.GetUserId()).Returns(1);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);

        var result = await _sut.GenerateNew2FARecoveryCodesAsync();

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Two-factor authentication is not enabled.");
    }

    [Fact]
    public async Task GenerateNew2FARecoveryCodesAsync_WhenSuccessful_ReturnsTenCodes()
    {
        var user = MakeUser(1);
        user.TwoFactorEnabled = true;
        var codes = Enumerable.Range(1, 10).Select(i => $"code{i}").ToList();
        _currentUserService.Setup(s => s.GetUserId()).Returns(1);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
                    .ReturnsAsync(codes);

        var result = await _sut.GenerateNew2FARecoveryCodesAsync();

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().HaveCount(10);
        result.Messages.Should().ContainSingle().Which.Should().Be("New recovery codes generated.");
    }

    // â”€â”€ LockUserAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task LockUserAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.LockUserAsync(99);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task LockUserAsync_WhenSeedAdmin_ReturnsFail()
    {
        var admin = MakeUser(1, "admin@seed.com");
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(admin);
        _userManager.Setup(m => m.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

        var result = await _sut.LockUserAsync(1);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("Cannot lock the system administrator.");
    }

    [Fact]
    public async Task LockUserAsync_WhenSuccessful_InvalidatesRefreshTokenAndReturnsSuccess()
    {
        var user = MakeUser(2, "user@test.com");
        _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LockUserAsync(2);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User locked successfully.");
        user.RefreshTokenExpiryDate.Should().BeBefore(FixedNow);
    }

    // â”€â”€ UnlockUserAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UnlockUserAsync_WhenUserNotFound_ReturnsFail()
    {
        _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.UnlockUserAsync(99);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().ContainSingle().Which.Should().Be("User does not exist.");
    }

    [Fact]
    public async Task UnlockUserAsync_WhenSuccessful_ResetsFailedCountAndReturnsSuccess()
    {
        var user = MakeUser(2, "user@test.com");
        _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UnlockUserAsync(2);

        result.IsSuccessful.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("User unlocked successfully.");
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    // â”€â”€ Email Confirmation Links â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RegisterUserAsync_WhenAutoConfirmEmailFalse_SendsConfirmationEmailWithClientUrl()
    {
        // Arrange
        var req = new UserRegistrationRequest 
        { 
            Email = "new@test.com", 
            Password = "Pass@1", 
            FullName = "Alice", 
            AutoConfirmEmail = false, 
            ActivateUser = true 
        };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), req.Password)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Basic")).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("verification-token");
        
        SendEmailDto? sentEmailDto = null;
        _emailService.Setup(m => m.SendAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .Callback<SendEmailDto, CancellationToken>((dto, ct) => sentEmailDto = dto)
                     .ReturnsAsync(string.Empty);

        // Act
        var result = await _sut.RegisterUserAsync(req);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        sentEmailDto.Should().NotBeNull();
        sentEmailDto!.MailTo.Should().Be(req.Email);
        sentEmailDto.MessageBody.Should().Contain("http://client.example.com/confirm-email");
        sentEmailDto.MessageBody.Should().Contain("token=verification-token");
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_SendsConfirmationEmailWithClientUrl()
    {
        // Arrange
        var user = MakeUser(1, "u@t.com", confirmed: false);
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("resend-token");

        SendEmailDto? sentEmailDto = null;
        _emailService.Setup(m => m.SendAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .Callback<SendEmailDto, CancellationToken>((dto, ct) => sentEmailDto = dto)
                     .ReturnsAsync(string.Empty);

        // Act
        var result = await _sut.ResendConfirmationEmailAsync(user.Email!);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        sentEmailDto.Should().NotBeNull();
        sentEmailDto!.MailTo.Should().Be(user.Email);
        sentEmailDto.MessageBody.Should().Contain("http://client.example.com/confirm-email");
        sentEmailDto.MessageBody.Should().Contain("token=resend-token");
    }

    [Fact]
    public async Task GenerateChangeEmailTokenAsync_SendsConfirmationEmailWithClientUrl()
    {
        // Arrange
        var user = MakeUser(1, "old@test.com");
        _currentUserService.Setup(s => s.GetUserId()).Returns(1);
        _userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@test.com")).ReturnsAsync("change-token");

        SendEmailDto? sentEmailDto = null;
        _emailService.Setup(m => m.SendAsync(It.IsAny<SendEmailDto>(), It.IsAny<CancellationToken>()))
                     .Callback<SendEmailDto, CancellationToken>((dto, ct) => sentEmailDto = dto)
                     .ReturnsAsync(string.Empty);

        // Act
        var result = await _sut.GenerateChangeEmailTokenAsync("new@test.com");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        sentEmailDto.Should().NotBeNull();
        sentEmailDto!.MailTo.Should().Be(user.Email);
        sentEmailDto.MessageBody.Should().Contain("http://client.example.com/confirm-email-change");
        sentEmailDto.MessageBody.Should().Contain("token=change-token");
    }
}