namespace be.Security;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Member = "member";

    private static readonly string[] PrimaryRolePriority =
    [
        Admin,
        Member
    ];

    public static string GetPrimaryRole(IEnumerable<string> roles)
    {
        var roleList = roles.ToArray();
        var roleSet = roleList.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var role in PrimaryRolePriority)
        {
            if (roleSet.Contains(role))
            {
                return role;
            }
        }

        return roleList
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? Member;
    }
}
