using System.Collections.ObjectModel;

namespace WFAI.Application.Authorization
{
    public static class AppAction
    {
        public const string Create = nameof(Create);
        public const string Read = nameof(Read);
        public const string Update = nameof(Update);
        public const string Delete = nameof(Delete);
        public const string Lock        = nameof(Lock);
        public const string Unlock      = nameof(Unlock);
        public const string ChangeEmail = nameof(ChangeEmail);
        public const string Manage2FA   = nameof(Manage2FA);
    }

    public static class AppFeature
    {
        public const string Users = nameof(Users);
        public const string Roles = nameof(Roles);
        public const string UserRoles = nameof(UserRoles);
        public const string RoleClaims = nameof(RoleClaims);
        public const string Menus = nameof(Menus);
        public const string Categories = nameof(Categories);
        public const string AuditTrails = nameof(AuditTrails);
        public const string Phases = nameof(Phases);
    }

    public static class AppService
    {
        public const string Identity = nameof(Identity);
        public const string Product = nameof(Product);
        public const string Website = nameof(Website);
    }

    public record AppPermission(string Service, string Feature, string Action, string Description, bool IsBasic = false)
    {
        public string Name => NameFor(Service, Feature, Action);

        public static string NameFor(string service, string feature, string action)
        {
            return $"Permission.{service}.{feature}.{action}";
        }
    }

    public static class AppPermissions
    {
        private static readonly AppPermission[] All =
        [
            new(AppService.Identity, AppFeature.Users, AppAction.Create, "Create Users"),
            new(AppService.Identity, AppFeature.Users, AppAction.Read, "Read Users"),
            new(AppService.Identity, AppFeature.Users, AppAction.Update, "Update Users"),
            new(AppService.Identity, AppFeature.Users, AppAction.Delete, "Delete Users"),
            new(AppService.Identity, AppFeature.Roles, AppAction.Create, "Create Roles"),
            new(AppService.Identity, AppFeature.Roles, AppAction.Read, "Read Roles"),
            new(AppService.Identity, AppFeature.Roles, AppAction.Update, "Update Roles"),
            new(AppService.Identity, AppFeature.Roles, AppAction.Delete, "Delete Roles"),
            new(AppService.Identity, AppFeature.UserRoles, AppAction.Read, "Read User Roles"),
            new(AppService.Identity, AppFeature.UserRoles, AppAction.Update, "Update User Roles"),
            new(AppService.Identity, AppFeature.RoleClaims, AppAction.Read, "Read Role Claims/Permissions"),
            new(AppService.Identity, AppFeature.RoleClaims, AppAction.Update, "Update Role Claims/Permissions"),
            new(AppService.Product, AppFeature.Categories, AppAction.Create, "Create Categories"),
            new(AppService.Product, AppFeature.Categories, AppAction.Read, "Read Categories", IsBasic: true),
            new(AppService.Product, AppFeature.Categories, AppAction.Update, "Update Categories"),
            new(AppService.Product, AppFeature.Categories, AppAction.Delete, "Delete Categories"),
            new(AppService.Product, AppFeature.Phases, AppAction.Create, "Create Phases"),
            new(AppService.Product, AppFeature.Phases, AppAction.Read, "Read Phases", IsBasic: true),
            new(AppService.Product, AppFeature.Phases, AppAction.Update, "Update Phases"),
            new(AppService.Product, AppFeature.Phases, AppAction.Delete, "Delete Phases"),
            new(AppService.Identity, AppFeature.Users, AppAction.Lock,        "Lock Users"),
            new(AppService.Identity, AppFeature.Users, AppAction.Unlock,      "Unlock Users"),
            new(AppService.Identity, AppFeature.Users, AppAction.ChangeEmail, "Change User Email", IsBasic: true),
            new(AppService.Identity, AppFeature.Users, AppAction.Manage2FA,   "Manage User 2FA",   IsBasic: true),
            new(AppService.Identity, AppFeature.AuditTrails, AppAction.Read,  "Read Audit Trails"),
        ];

        public static IReadOnlyList<AppPermission> AllPermissions { get; } =
            new ReadOnlyCollection<AppPermission>(All);

        public static IReadOnlyList<AppPermission> AdminPermissions { get; } =
            new ReadOnlyCollection<AppPermission>(All.Where(p => !p.IsBasic).ToArray());

        public static IReadOnlyList<AppPermission> BasicPermissions { get; } =
            new ReadOnlyCollection<AppPermission>(All.Where(p => p.IsBasic).ToArray());
    }
}