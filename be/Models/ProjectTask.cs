namespace be.Models;

public sealed class ProjectTask
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
