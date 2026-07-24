using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WFAI.Infrastructure.Identity.Models;

namespace WFAI.Infrastructure.Tests.Support;

internal static class IdentityMockFactory
{
    public static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(x => x.Value).Returns(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            keyNormalizer.Object,
            errors,
            null!,
            logger.Object);
    }

    public static Mock<RoleManager<ApplicationRole>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<RoleManager<ApplicationRole>>>();

        return new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            keyNormalizer.Object,
            errors,
            logger.Object);
    }
}