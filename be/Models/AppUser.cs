using Microsoft.AspNetCore.Identity;

namespace be.Models;

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public List<Project> Projects { get; set; } = new();
}
