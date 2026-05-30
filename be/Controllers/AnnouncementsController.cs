using System.ComponentModel.DataAnnotations;
using be.Contracts;
using be.Data;
using be.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Authorize]
[Route("api/announcements")]
public class AnnouncementsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AnnouncementResponse>>> GetAnnouncements(
        [FromQuery] PaginationRequest request)
    {
        return await db.Announcements
            .AsNoTracking()
            .OrderByDescending(announcement => announcement.PublishedAt)
            .Select(announcement => new AnnouncementResponse(
                announcement.Id,
                announcement.Title,
                announcement.Body,
                announcement.PublishedAt))
            .ToPagedResponseAsync(request.Page, request.PageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnnouncementResponse>> GetAnnouncement(Guid id)
    {
        var announcement = await db.Announcements
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new AnnouncementResponse(
                candidate.Id,
                candidate.Title,
                candidate.Body,
                candidate.PublishedAt))
            .SingleOrDefaultAsync();

        return announcement is null ? NotFound() : announcement;
    }

    [HttpPost]
    public async Task<ActionResult<AnnouncementResponse>> CreateAnnouncement(
        CreateAnnouncementRequest request)
    {
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            PublishedAt = request.PublishedAt ?? DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        var response = new AnnouncementResponse(
            announcement.Id,
            announcement.Title,
            announcement.Body,
            announcement.PublishedAt);

        return CreatedAtAction(nameof(GetAnnouncement), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AnnouncementResponse>> UpdateAnnouncement(
        Guid id,
        UpdateAnnouncementRequest request)
    {
        var announcement = await db.Announcements.SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        announcement.Title = request.Title.Trim();
        announcement.Body = request.Body.Trim();
        announcement.PublishedAt = request.PublishedAt ?? announcement.PublishedAt;

        await db.SaveChangesAsync();

        return new AnnouncementResponse(
            announcement.Id,
            announcement.Title,
            announcement.Body,
            announcement.PublishedAt);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id)
    {
        var announcement = await db.Announcements.SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public sealed record AnnouncementResponse(
    Guid Id,
    string Title,
    string Body,
    DateTime PublishedAt);

public sealed record CreateAnnouncementRequest(
    [property: Required, StringLength(160, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Title,
    [property: Required, StringLength(2000, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Body,
    DateTime? PublishedAt);

public sealed record UpdateAnnouncementRequest(
    [property: Required, StringLength(160, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Title,
    [property: Required, StringLength(2000, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Body,
    DateTime? PublishedAt);
