using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WFAI.Application.Features.Categories;
using WFAI.Application.Features.Categories.Commands.Create;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Infrastructure.Persistence.Contexts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Support;

public sealed class ApiTestDataSeeder
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiTestDataSeeder(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task<Category> SeedCategoryAsync(
        string name,
        string slug,
        bool isActive = true,
        int sortOrder = 1,
        int? parentId = null,
        bool softDeleted = false,
        CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var response = await sender.Send(
            new CreateCategoryCommand(name, slug, parentId, isActive, sortOrder),
            ct);

        if (!response.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"Failed to seed category '{name}': {string.Join("; ", response.Messages)}");
        }

        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .SingleAsync(c => c.Id == response.Data, ct);

        if (softDeleted)
        {
            category.SoftDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = 1;
            await dbContext.SaveChangesAsync(ct);
        }

        return category;
    }

    public void ClearCategoryCaches()
    {
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        foreach (var key in CategoryCacheKeys.All)
        {
            cacheService.Remove(key);
        }
    }

    public async Task<ApplicationUser> SeedUserAsync(
        string email,
        string password,
        IEnumerable<string>? roleNames = null,
        CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = "API Test Seed User",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            RefreshToken = Guid.NewGuid().ToString("N"),
            RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(1),
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed API test user '{email}': {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        if (roleNames is not null)
        {
            var addRolesResult = await userManager.AddToRolesAsync(user, roleNames);
            if (!addRolesResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign roles to API test user '{email}': {string.Join("; ", addRolesResult.Errors.Select(error => error.Description))}");
            }
        }

        return await userManager.Users.SingleAsync(createdUser => createdUser.Email == email, ct);
    }

    public async Task<ApplicationUser> SeedUnconfirmedUserAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return existingUser;

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = "Unconfirmed Test User",
            EmailConfirmed = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            RefreshToken = Guid.NewGuid().ToString("N"),
            RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(1),
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed unconfirmed user '{email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

        return await userManager.Users.SingleAsync(u => u.Email == email, ct);
    }

    public async Task<ApplicationRole> SeedRoleAsync(
        string roleName,
        string description,
        CancellationToken ct = default)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var existingRole = await roleManager.FindByNameAsync(roleName);
        if (existingRole is not null)
        {
            return existingRole;
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = description
        };

        var createResult = await roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed role '{roleName}': {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        return await roleManager.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Seeded role '{roleName}' could not be reloaded.");
    }
}