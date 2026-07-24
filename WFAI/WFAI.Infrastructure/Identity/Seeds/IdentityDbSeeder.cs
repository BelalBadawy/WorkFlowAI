using Microsoft.Extensions.Options;
using WFAI.Application.Authorization;
using WFAI.Infrastructure.Identity.Configurations;

namespace WFAI.Infrastructure.Persistence.DbInitializers
{
    public class IdentityDbSeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly SeedUsersConfiguration _seedUsersConfiguration;

        public IdentityDbSeeder(
            ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IOptions<SeedUsersConfiguration> seedUsersConfiguration)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _seedUsersConfiguration = seedUsersConfiguration.Value;
        }

        public async Task SeedIdentityDatabaseAsync()
        {
            await CheckAndApplyPendingMigrationAsync();
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedBasicUserAsync();
        }

        private async Task CheckAndApplyPendingMigrationAsync()
        {
            if ((await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                await _context.Database.MigrateAsync();
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var adminConfiguration = _seedUsersConfiguration.Admin;
            var user = new ApplicationUser
            {
                FullName = adminConfiguration.FullName,
                Email = adminConfiguration.Email,
                UserName = adminConfiguration.Email,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = adminConfiguration.PhoneNumber,
                NormalizedEmail = adminConfiguration.Email.ToUpperInvariant(),
                NormalizedUserName = adminConfiguration.Email.ToUpperInvariant(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                RefreshToken = Guid.NewGuid().ToString("N"),
                RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            if (!await _userManager.Users.AnyAsync(u => u.Email == adminConfiguration.Email))
            {
                await _userManager.CreateAsync(user, adminConfiguration.Password);
            }

            user = await _userManager.FindByEmailAsync(adminConfiguration.Email);
            if (!await _userManager.IsInRoleAsync(user, AppRoles.Basic)
                && !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                await _userManager.AddToRolesAsync(user, AppRoles.DefaultRoles);
            }
        }

        private async Task SeedBasicUserAsync()
        {
            var basicConfiguration = _seedUsersConfiguration.Basic;
            var email = basicConfiguration.Email;
            var user = new ApplicationUser
            {
                FullName = basicConfiguration.FullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                PhoneNumber = basicConfiguration.PhoneNumber,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                RefreshToken = Guid.NewGuid().ToString("N"),
                RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            if (!await _userManager.Users.AnyAsync(u => u.Email == email))
            {
                await _userManager.CreateAsync(user, basicConfiguration.Password);
            }

            user = await _userManager.FindByEmailAsync(email);

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Basic))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Basic);
            }
        }

        private async Task SeedRolesAsync()
        {
            foreach (var roleName in AppRoles.DefaultRoles)
            {
                if (await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleName) is not ApplicationRole role)
                {
                    role = new ApplicationRole
                    {
                        Name = roleName,
                        Description = $"{roleName} Role.",
                        NormalizedName = roleName.ToUpperInvariant()
                    };

                    await _roleManager.CreateAsync(role);
                }

                if (roleName == AppRoles.Basic)
                {
                    await AssignPermissionsToRoleAsync(role, AppPermissions.BasicPermissions);
                }
                else if (roleName == AppRoles.Admin)
                {
                    await AssignPermissionsToRoleAsync(role, AppPermissions.AllPermissions);
                }
            }
        }

        private async Task AssignPermissionsToRoleAsync(ApplicationRole role, IReadOnlyList<AppPermission> permmisions)
        {
            var currentlyAssignedClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var permission in permmisions)
            {
                if (!currentlyAssignedClaims.Any(claim => claim.Type == AppClaim.Permission && claim.Value == permission.Name))
                {
                    await _context.RoleClaims.AddAsync(new ApplicationRoleClaim
                    {
                        RoleId = role.Id,
                        ClaimType = AppClaim.Permission,
                        ClaimValue = permission.Name,
                        Description = permission.Description
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}