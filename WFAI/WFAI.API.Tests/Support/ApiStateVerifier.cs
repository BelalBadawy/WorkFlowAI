using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WFAI.Domain.Entities;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Identity.Constants;
using WFAI.Infrastructure.Persistence.Contexts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Support;

public sealed class ApiStateVerifier
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiStateVerifier(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == categoryId, ct);
    }

    public async Task<Category?> GetCategoryByIdIncludingSoftDeletedAsync(int categoryId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == categoryId, ct);
    }

    public async Task<ApplicationRole?> GetRoleByIdAsync(int roleId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == roleId, ct);
    }

    public async Task<List<ApplicationRoleClaim>> GetRoleClaimsAsync(int roleId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.RoleClaims
            .AsNoTracking()
            .Where(roleClaim => roleClaim.RoleId == roleId && roleClaim.ClaimType == AppClaim.Permission)
            .ToListAsync(ct);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int userId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, ct);
    }

    public async Task<List<string>> GetUserRoleNamesAsync(int userId, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await (from userRole in dbContext.UserRoles
                      join role in dbContext.Roles on userRole.RoleId equals role.Id
                      where userRole.UserId == userId
                      select role.Name!)
            .ToListAsync(ct);
    }

    public async Task<AuditTrail?> GetLastAuditTrailForTableAsync(string tableName, CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.AuditTrails
            .AsNoTracking()
            .Where(x => x.TableName == tableName)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }
}