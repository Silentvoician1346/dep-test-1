using be.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/project-task-report")]
public class AdminProjectTaskReportController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectTaskReportResponse>> Get()
    {
        var rows = await (
            from user in db.AppUsers.AsNoTracking()
            join project in db.Projects.AsNoTracking()
                on user.Id equals project.OwnerId
            join task in db.ProjectTasks.AsNoTracking()
                on project.Id equals task.ProjectId
            orderby user.Email, project.Name, task.Title
            select new ProjectTaskReportRow(
                user.Id,
                user.Email,
                user.DisplayName,
                project.Id,
                project.Name,
                project.Status,
                task.Id,
                task.Title,
                task.IsDone))
            .ToListAsync();

        return new ProjectTaskReportResponse(rows.Count, rows);
    }
}

public sealed record ProjectTaskReportResponse(
    int TotalRows,
    IReadOnlyCollection<ProjectTaskReportRow> Rows);

public sealed record ProjectTaskReportRow(
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    Guid ProjectId,
    string ProjectName,
    string ProjectStatus,
    Guid TaskId,
    string TaskTitle,
    bool TaskIsDone);
