using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WFAI.Infrastructure.Services.Common;

namespace WFAI.Infrastructure.Tests.Services.Common;

public class CurrentUserServiceTests
{
    private static ClaimsPrincipal AuthenticatedPrincipal(
        int userId = 7,
        string email = "user@test.com",
        string name = "Test User",
        string role = "Admin")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestScheme"));
    }

    private static CurrentUserService BuildService(ClaimsPrincipal? principal = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        if (principal is not null)
        {
            var ctx = new DefaultHttpContext { User = principal };
            accessor.Setup(a => a.HttpContext).Returns(ctx);
        }
        else
        {
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }

        return new CurrentUserService(accessor.Object);
    }

    [Fact]
    public void User_WhenHttpContextHasUser_ReturnsHttpContextUser()
    {
        var principal = AuthenticatedPrincipal();
        var sut = BuildService(principal);

        sut.User.Should().Be(principal);
    }

    [Fact]
    public void User_WhenExplicitPrincipalIsSet_OverridesHttpContextUser()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, "original@test.com"));
        var overridePrincipal = AuthenticatedPrincipal(99, "override@test.com");

        sut.SetCurrentUser(overridePrincipal);

        sut.User.Should().Be(overridePrincipal);
    }

    [Fact]
    public void GetUserId_WhenNameIdentifierClaimPresent_ReturnsId()
    {
        var sut = BuildService(AuthenticatedPrincipal(42));

        sut.GetUserId().Should().Be(42);
    }

    [Fact]
    public void GetUserId_WhenNoHttpContext_ReturnsNull()
    {
        var sut = BuildService(null);

        sut.GetUserId().Should().BeNull();
    }

    [Fact]
    public void GetUserEmail_WhenAuthenticated_ReturnsEmail()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, "alice@example.com"));

        sut.GetUserEmail().Should().Be("alice@example.com");
    }

    [Fact]
    public void GetUserEmail_WhenNoHttpContext_ReturnsEmptyString()
    {
        var sut = BuildService(null);

        sut.GetUserEmail().Should().BeEmpty();
    }

    [Fact]
    public void IsAuthenticated_WhenContextHasAuthenticatedPrincipal_ReturnsTrue()
    {
        var sut = BuildService(AuthenticatedPrincipal());

        sut.IsAuthenticated().Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenNoHttpContext_ReturnsFalse()
    {
        var sut = BuildService(null);

        sut.IsAuthenticated().Should().BeFalse();
    }

    [Fact]
    public void GetRoles_WhenUserHasRoleClaim_ReturnsRoles()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, role: "Editor"));

        sut.GetRoles().Should().ContainSingle().Which.Should().Be("Editor");
    }

    [Fact]
    public void GetClaims_WhenAuthenticated_ReturnsAllClaims()
    {
        var sut = BuildService(AuthenticatedPrincipal());

        sut.GetClaims().Should().NotBeEmpty();
    }

    [Fact]
    public void HasRole_WhenUserIsInRole_ReturnsTrue()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, role: "Admin"));

        sut.HasRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void HasRole_WhenUserIsNotInRole_ReturnsFalse()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, role: "Basic"));

        sut.HasRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void HasClaim_WhenUserHasClaim_ReturnsTrue()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, role: "Admin"));

        sut.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
    }

    [Fact]
    public void HasClaim_WhenUserDoesNotHaveClaim_ReturnsFalse()
    {
        var sut = BuildService(AuthenticatedPrincipal(1, role: "Basic"));

        sut.HasClaim(ClaimTypes.Role, "Admin").Should().BeFalse();
    }

    [Fact]
    public void Name_WhenContextHasNameClaim_ReturnsName()
    {
        var sut = BuildService(AuthenticatedPrincipal(name: "Bob Smith"));

        sut.Name.Should().Be("Bob Smith");
    }
}