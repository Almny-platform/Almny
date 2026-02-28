namespace Almny.Api.Authentication;

public static class Permissions
{
    public const string ViewUsers = "users:view";
    public const string ManageUsers = "users:manage";
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class RolePermissions
{
    private static readonly Dictionary<string, IReadOnlyList<string>> _rolePermissions = new()
    {
        ["Admin"] = [Permissions.ViewUsers, Permissions.ManageUsers],
        ["User"] = [Permissions.ViewUsers]
    };

    public static IReadOnlyList<string> GetPermissionsForRole(string role) =>
        _rolePermissions.TryGetValue(role, out var permissions) ? permissions : [];

    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles) =>
        roles.SelectMany(GetPermissionsForRole).Distinct().ToList();
}
