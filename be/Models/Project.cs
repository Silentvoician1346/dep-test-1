namespace be.Models;

public sealed class Project
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public AppUser Owner { get; set; } = null!;

    public List<ProjectTask> Tasks { get; set; } = new();
}
