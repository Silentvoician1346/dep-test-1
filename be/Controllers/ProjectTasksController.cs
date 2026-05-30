using System.ComponentModel.DataAnnotations;
using be.Contracts;
using be.Data;
using be.Extensions;
using be.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Authorize]
[Route("api/project-tasks")]
public class ProjectTasksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectTaskResponse>>> GetProjectTasks(
        [FromQuery] ProjectTaskListRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var query = db.ProjectTasks
            .AsNoTracking()
            .Where(task => isAdmin || task.Project.OwnerId == userId);

        if (request.ProjectId is not null)
        {
            var canReadProject = await CanAccessProjectAsync(request.ProjectId.Value, userId, isAdmin);

            if (!canReadProject)
            {
                return Forbid();
            }

            query = query.Where(task => task.ProjectId == request.ProjectId);
        }

        return await query
            .OrderBy(task => task.Title)
            .Select(task => new ProjectTaskResponse(
                task.Id,
                task.ProjectId,
                task.Project.Name,
                task.Title,
                task.IsDone,
                task.CreatedAt))
            .ToPagedResponseAsync(request.Page, request.PageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectTaskResponse>> GetProjectTask(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var task = await db.ProjectTasks
            .AsNoTracking()
            .Where(candidate => candidate.Id == id && (isAdmin || candidate.Project.OwnerId == userId))
            .Select(candidate => new ProjectTaskResponse(
                candidate.Id,
                candidate.ProjectId,
                candidate.Project.Name,
                candidate.Title,
                candidate.IsDone,
                candidate.CreatedAt))
            .SingleOrDefaultAsync();

        return task is null ? NotFound() : task;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectTaskResponse>> CreateProjectTask(CreateProjectTaskRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var canAccessProject = await CanAccessProjectAsync(request.ProjectId, userId, isAdmin);

        if (!canAccessProject)
        {
            return Forbid();
        }

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            IsDone = request.IsDone,
            CreatedAt = DateTime.UtcNow
        };

        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync();

        var response = await db.ProjectTasks
            .AsNoTracking()
            .Where(candidate => candidate.Id == task.Id)
            .Select(candidate => new ProjectTaskResponse(
                candidate.Id,
                candidate.ProjectId,
                candidate.Project.Name,
                candidate.Title,
                candidate.IsDone,
                candidate.CreatedAt))
            .SingleAsync();

        return CreatedAtAction(nameof(GetProjectTask), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectTaskResponse>> UpdateProjectTask(
        Guid id,
        UpdateProjectTaskRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var task = await db.ProjectTasks
            .Include(candidate => candidate.Project)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == id && (isAdmin || candidate.Project.OwnerId == userId));

        if (task is null)
        {
            return NotFound();
        }

        task.Title = request.Title.Trim();
        task.IsDone = request.IsDone;

        await db.SaveChangesAsync();

        return new ProjectTaskResponse(
            task.Id,
            task.ProjectId,
            task.Project.Name,
            task.Title,
            task.IsDone,
            task.CreatedAt);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProjectTask(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var task = await db.ProjectTasks.SingleOrDefaultAsync(candidate =>
            candidate.Id == id && (isAdmin || candidate.Project.OwnerId == userId));

        if (task is null)
        {
            return NotFound();
        }

        db.ProjectTasks.Remove(task);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private Task<bool> CanAccessProjectAsync(Guid projectId, Guid userId, bool isAdmin)
    {
        return db.Projects.AnyAsync(project =>
            project.Id == projectId && (isAdmin || project.OwnerId == userId));
    }
}

public sealed class ProjectTaskListRequest : PaginationRequest
{
    public Guid? ProjectId { get; set; }
}

public sealed record ProjectTaskResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Title,
    bool IsDone,
    DateTime CreatedAt);

public sealed record CreateProjectTaskRequest(
    Guid ProjectId,
    [property: Required, StringLength(200, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Title,
    bool IsDone = false);

public sealed record UpdateProjectTaskRequest(
    [property: Required, StringLength(200, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Title,
    bool IsDone);
