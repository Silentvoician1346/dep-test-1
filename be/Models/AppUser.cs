namespace be.Models;

public sealed class AppUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "member";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public List<Project> Projects { get; set; } = new();
}
