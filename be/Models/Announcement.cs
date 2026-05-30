namespace be.Models;

public sealed class Announcement
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }
}
