using be.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/database-overview")]
public class DatabaseOverviewController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DatabaseOverviewResponse>> Get()
    {
        var users = await db.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new UserSummary(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role,
                user.IsActive,
                user.Projects.Count))
            .ToListAsync();

        var projectRows = await db.Projects
            .AsNoTracking()
            .Include(project => project.Owner)
            .Include(project => project.Tasks)
            .OrderBy(project => project.Name)
            .ToListAsync();

        var projects = projectRows
            .Select(project => new ProjectSummary(
                project.Id,
                project.Name,
                project.Status,
                project.Owner.Email,
                project.Tasks
                    .OrderBy(task => task.Title)
                    .Select(task => new TaskSummary(task.Id, task.Title, task.IsDone))
                    .ToList()))
            .ToList();

        var announcements = await db.Announcements
            .AsNoTracking()
            .OrderByDescending(announcement => announcement.PublishedAt)
            .Select(announcement => new AnnouncementSummary(
                announcement.Id,
                announcement.Title,
                announcement.PublishedAt))
            .ToListAsync();

        return new DatabaseOverviewResponse(users, projects, announcements);
    }
}

public sealed record DatabaseOverviewResponse(
    IReadOnlyCollection<UserSummary> Users,
    IReadOnlyCollection<ProjectSummary> Projects,
    IReadOnlyCollection<AnnouncementSummary> Announcements);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    int ProjectCount);

public sealed record ProjectSummary(
    Guid Id,
    string Name,
    string Status,
    string OwnerEmail,
    IReadOnlyCollection<TaskSummary> Tasks);

public sealed record TaskSummary(Guid Id, string Title, bool IsDone);

public sealed record AnnouncementSummary(Guid Id, string Title, DateTime PublishedAt);
