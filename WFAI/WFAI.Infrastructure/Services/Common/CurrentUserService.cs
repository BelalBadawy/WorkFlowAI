using WFAI.Application.Interfaces.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WFAI.Infrastructure.Services.Common;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? _explicitUser;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? User => _explicitUser ?? _httpContextAccessor.HttpContext?.User;

    public string Name => User?.Identity?.Name ?? string.Empty;

    public int? GetUserId()
    {
        var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : null;
    }

    public string GetUserEmail() => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public bool IsAuthenticated() => User?.Identity?.IsAuthenticated ?? false;

    public IList<string> GetRoles() => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public IList<Claim> GetClaims() => User?.Claims.ToList() ?? new List<Claim>();

    public bool HasRole(string roleName) => User?.IsInRole(roleName) ?? false;

    public bool HasClaim(string claimType, string value) => User?.HasClaim(claimType, value) ?? false;

    public void SetCurrentUser(ClaimsPrincipal principal)
    {
        _explicitUser = principal;
    }

    public string? GetIpAddress() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}