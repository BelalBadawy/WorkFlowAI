using Mapster;
using WFAI.Application.Authorization;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Identity.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;

        public RoleService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, IApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IResponseWrapper> CreateRoleAsync(CreateRoleRequest createRole)
        {
            var roleInDb = await _roleManager.FindByNameAsync(createRole.Name);
            if (roleInDb is not null)
            {
                return ResponseWrapper.Fail("Role already exists");
            }

            var newRole = new ApplicationRole
            {
                Name = createRole.Name,
                Description = createRole.Description
            };

            var identityResult = await _roleManager.CreateAsync(newRole);

            if (identityResult.Succeeded)
            {
                return ResponseWrapper.Success(message: "Role created successfully");
            }

            return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
        }

        public async Task<IResponseWrapper> DeleteRoleAsync(int roleId)
        {
            if (roleId == 0)
            {
                return ResponseWrapper.Fail("Role Id is required.");
            }

            var roleInDb = await _roleManager.FindByIdAsync(roleId.ToString());

            if (roleInDb is not null)
            {
                if (!string.Equals(roleInDb.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(roleInDb.Name);

                    if (usersInRole.Any())
                    {
                        return ResponseWrapper.Fail($"Role: {roleInDb.Name} is currently assigned to a user.");
                    }

                    var identityResult = await _roleManager.DeleteAsync(roleInDb);

                    if (identityResult.Succeeded)
                    {
                        return ResponseWrapper.Success("Role successfully deleted.");
                    }

                    return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
                }

                return ResponseWrapper.Fail("Cannot delete Admin role.");
            }

            return ResponseWrapper.Fail("Role does not exist.");
        }

        public async Task<IResponseWrapper<RoleClaimResponse>> GetPermissionsAsync(int roleId)
        {
            var roleInDb = await _roleManager.FindByIdAsync(roleId.ToString());

            if (roleInDb is not null)
            {
                var allPermissions = AppPermissions.AllPermissions;

                var roleClaimResponse = new RoleClaimResponse
                {
                    Role = new RoleResponse
                    {
                        Id = roleId,
                        Name = roleInDb.Name,
                        Description = roleInDb.Description
                    },
                    RoleClaims = new List<RoleClaimViewModel>()
                };

                var currentlyAssignedClaims = await GetAllClaimsForRoleAsync(roleId);

                var allPermissionNames = allPermissions.Select(p => p.Name).ToList();

                var currentlyAssignedClaimsValues = currentlyAssignedClaims
                    .Select(rc => rc.ClaimValue).ToList();

                var currentlyAssignedRoleClaimsNames = allPermissionNames
                    .Intersect(currentlyAssignedClaimsValues)
                    .ToList();

                foreach (var permission in allPermissions)
                {
                    roleClaimResponse.RoleClaims.Add(new RoleClaimViewModel
                    {
                        ClaimType = AppClaim.Permission,
                        ClaimValue = permission.Name,
                        Description = permission.Description,
                        Selected = currentlyAssignedRoleClaimsNames.Contains(permission.Name)
                    });
                }

                return ResponseWrapper<RoleClaimResponse>.Success(data: roleClaimResponse);
            }

            return ResponseWrapper<RoleClaimResponse>.Fail(message: "Role does not exist.");
        }

        public async Task<IResponseWrapper<RoleResponse>> GetRoleByIdAsync(int roleId)
        {
            var roleInDb = await _roleManager.FindByIdAsync(roleId.ToString());

            if (roleInDb is not null)
            {
                var mappedRole = roleInDb.Adapt<RoleResponse>();

                return ResponseWrapper<RoleResponse>.Success(data: mappedRole);
            }

            return ResponseWrapper<RoleResponse>.Fail("Role does not exist.");
        }

        public async Task<IResponseWrapper<List<RoleResponse>>> GetRolesAsync()
        {
            var allRoles = await _roleManager.Roles.ToListAsync();

            if (allRoles.Count > 0)
            {
                var mappedRoles = allRoles.Adapt<List<RoleResponse>>();

                return ResponseWrapper<List<RoleResponse>>.Success(data: mappedRoles);
            }

            return ResponseWrapper<List<RoleResponse>>.Fail("No roles were found.");
        }

        public async Task<IResponseWrapper> UpdateRoleAsync(UpdateRoleRequest updateRole)
        {
            var roleInDb = await _roleManager.FindByIdAsync(updateRole.RoleId.ToString());

            if (roleInDb is not null)
            {
                if (!string.Equals(roleInDb.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    roleInDb.Name = updateRole.Name;
                    roleInDb.Description = updateRole.Description;

                    var identityResult = await _roleManager.UpdateAsync(roleInDb);

                    if (identityResult.Succeeded)
                    {
                        return ResponseWrapper.Success("Role updated successfully");
                    }

                    return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
                }

                return ResponseWrapper.Fail("Cannot update Admin role.");
            }

            return ResponseWrapper.Fail("Role does not exist.");
        }

        public async Task<IResponseWrapper> UpdateRolePermissionsAsync(UpdateRoleClaimsRequest updateRoleClaims)
        {
            var roleInDb = await _roleManager.FindByIdAsync(updateRoleClaims.RoleId.ToString());
            if (roleInDb is null)
                return ResponseWrapper.Fail("Role does not exist.");

            if (string.Equals(roleInDb.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
                return ResponseWrapper.Fail("Cannot change permissions for this role.");

            var allowedValues = AppPermissions.AllPermissions
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newClaims = updateRoleClaims.RoleClaims
                .Where(rc => rc.ClaimValue != null && allowedValues.Contains(rc.ClaimValue))
                .Select(rc => new Claim(AppClaim.Permission, rc.ClaimValue!))
                .ToList();

            var existingClaims = await _roleManager.GetClaimsAsync(roleInDb);

            var claimsToAdd = newClaims
                .Where(nc => !existingClaims.Any(ec => ec.Type == nc.Type && ec.Value == nc.Value))
                .ToList();

            var claimsToRemove = existingClaims
                .Where(ec => !newClaims.Any(nc => nc.Type == ec.Type && nc.Value == ec.Value))
                .ToList();

            if (!claimsToAdd.Any() && !claimsToRemove.Any())
                return ResponseWrapper.Success("No changes detected.");

            try
            {
                await _context.StartTransaction();

                foreach (var claim in claimsToRemove)
                    await _roleManager.RemoveClaimAsync(roleInDb, claim);

                foreach (var claim in claimsToAdd)
                    await _roleManager.AddClaimAsync(roleInDb, claim);

                await _context.CommitTransaction();
            }
            catch
            {
                await _context.RollbackTransaction();
                throw;
            }

            return ResponseWrapper.Success("Role permissions updated successfully.");
        }

        private List<string> GetIdentityResultErrorDescriptions(IdentityResult identityResult)
        {
            var errorDescriptions = new List<string>();
            foreach (var error in identityResult.Errors)
            {
                errorDescriptions.Add(error.Description);
            }

            return errorDescriptions;
        }

        private async Task<List<RoleClaimViewModel>> GetAllClaimsForRoleAsync(int roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null) return [];

            var claims = await _roleManager.GetClaimsAsync(role);
            return claims
                .Select(c => new RoleClaimViewModel { ClaimType = c.Type, ClaimValue = c.Value })
                .ToList();
        }
    }
}