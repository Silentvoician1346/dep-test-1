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
[Route("api/projects")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> GetProjects(
        [FromQuery] ProjectListRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var query = db.Projects
            .AsNoTracking()
            .Where(project => isAdmin || project.OwnerId == userId);

        if (request.OwnerId is not null)
        {
            if (!isAdmin && request.OwnerId != userId)
            {
                return Forbid();
            }

            query = query.Where(project => project.OwnerId == request.OwnerId);
        }

        return await query
            .OrderBy(project => project.Name)
            .Select(project => new ProjectResponse(
                project.Id,
                project.OwnerId,
                project.Owner.Email ?? string.Empty,
                project.Name,
                project.Status,
                project.CreatedAt,
                project.Tasks.Count))
            .ToPagedResponseAsync(request.Page, request.PageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var project = await db.Projects
            .AsNoTracking()
            .Where(candidate => candidate.Id == id && (isAdmin || candidate.OwnerId == userId))
            .Select(candidate => new ProjectResponse(
                candidate.Id,
                candidate.OwnerId,
                candidate.Owner.Email ?? string.Empty,
                candidate.Name,
                candidate.Status,
                candidate.CreatedAt,
                candidate.Tasks.Count))
            .SingleOrDefaultAsync();

        return project is null ? NotFound() : project;
    }

    [HttpGet("task-joins")]
    public async Task<ActionResult<PagedResponse<ProjectTaskJoinResponse>>> GetProjectTaskJoins(
        [FromQuery] ProjectTaskJoinListRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        if (!isAdmin && request.UserId is not null && request.UserId != userId)
        {
            return Forbid();
        }

        var requestedUserId = isAdmin ? request.UserId : userId;

        var query =
            from project in db.Projects.AsNoTracking()
            join task in db.ProjectTasks.AsNoTracking()
                on project.Id equals task.ProjectId
            where isAdmin || project.OwnerId == userId
            where requestedUserId == null || project.OwnerId == requestedUserId
            orderby project.Name, task.Title
            select new ProjectTaskJoinResponse(
                project.OwnerId,
                project.Id,
                project.Name,
                project.Status,
                task.Id,
                task.Title,
                task.IsDone,
                task.CreatedAt);

        return await query.ToPagedResponseAsync(request.Page, request.PageSize);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request)
    {
        var ownerId = User.IsAdmin() && request.OwnerId is not null
            ? request.OwnerId.Value
            : User.GetUserId();

        var ownerExists = await db.Users.AnyAsync(user => user.Id == ownerId && user.IsActive);

        if (!ownerExists)
        {
            return BadRequest(new { message = "Owner user does not exist or is inactive." });
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Status = NormalizeStatus(request.Status),
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var response = await db.Projects
            .AsNoTracking()
            .Where(candidate => candidate.Id == project.Id)
            .Select(candidate => new ProjectResponse(
                candidate.Id,
                candidate.OwnerId,
                candidate.Owner.Email ?? string.Empty,
                candidate.Name,
                candidate.Status,
                candidate.CreatedAt,
                candidate.Tasks.Count))
            .SingleAsync();

        return CreatedAtAction(nameof(GetProject), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(Guid id, UpdateProjectRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var project = await db.Projects.SingleOrDefaultAsync(candidate =>
            candidate.Id == id && (isAdmin || candidate.OwnerId == userId));

        if (project is null)
        {
            return NotFound();
        }

        project.Name = request.Name.Trim();
        project.Status = NormalizeStatus(request.Status);

        await db.SaveChangesAsync();

        return await db.Projects
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new ProjectResponse(
                candidate.Id,
                candidate.OwnerId,
                candidate.Owner.Email ?? string.Empty,
                candidate.Name,
                candidate.Status,
                candidate.CreatedAt,
                candidate.Tasks.Count))
            .SingleAsync();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var project = await db.Projects.SingleOrDefaultAsync(candidate =>
            candidate.Id == id && (isAdmin || candidate.OwnerId == userId));

        if (project is null)
        {
            return NotFound();
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? "active" : status.Trim();
    }
}

public sealed class ProjectListRequest : PaginationRequest
{
    public Guid? OwnerId { get; set; }
}

public sealed class ProjectTaskJoinListRequest : PaginationRequest
{
    public Guid? UserId { get; set; }
}

public sealed record ProjectResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerEmail,
    string Name,
    string Status,
    DateTime CreatedAt,
    int TaskCount);

public sealed record ProjectTaskJoinResponse(
    Guid UserId,
    Guid ProjectId,
    string ProjectName,
    string ProjectStatus,
    Guid TaskId,
    string TaskTitle,
    bool TaskIsDone,
    DateTime TaskCreatedAt);

public sealed record CreateProjectRequest(
    Guid? OwnerId,
    [property: Required, StringLength(160, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Name,
    [property: StringLength(50)] string? Status);

public sealed record UpdateProjectRequest(
    [property: Required, StringLength(160, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Name,
    [property: StringLength(50)] string? Status);
