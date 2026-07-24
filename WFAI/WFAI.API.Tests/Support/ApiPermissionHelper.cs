using WFAI.Application.Authorization;

namespace WFAI.API.Tests.Support;

public static class ApiPermissionHelper
{
    public static string GetRequiredPermission(string service, string feature, string action)
    {
        return AppPermission.NameFor(service, feature, action);
    }

    public static string GetWrongPermission(string requiredPermission)
    {
        var wrongPermission = AppPermissions.AllPermissions
            .Select(permission => permission.Name)
            .FirstOrDefault(permission => !string.Equals(permission, requiredPermission, StringComparison.Ordinal));

        return wrongPermission
            ?? throw new InvalidOperationException(
                $"No wrong permission could be selected for '{requiredPermission}'. Check AppPermissions.AllPermissions.");
    }
}