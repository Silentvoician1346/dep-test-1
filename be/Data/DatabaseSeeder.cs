using be.Models;
using be.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        var createdAt = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

        await EnsureRoleAsync(roleManager, AppRoles.Admin);
        await EnsureRoleAsync(roleManager, AppRoles.Member);

        var admin = await EnsureDemoUserAsync(
            userManager,
            Guid.Parse("6c693495-0308-40ee-a61c-cfe7caeecfa4"),
            "admin@example.local",
            "Local Admin",
            "Admin123!",
            AppRoles.Admin,
            createdAt);

        var member = await EnsureDemoUserAsync(
            userManager,
            Guid.Parse("ec2de4c6-b1e9-4873-a797-c7466d0396b1"),
            "member@example.local",
            "Local Member",
            "Member123!",
            AppRoles.Member,
            createdAt);

        await EnsureProjectAsync(
            db,
            Guid.Parse("bfb84013-89ce-4536-ac3d-81e7e5099fb5"),
            admin.Id,
            "Local Onboarding",
            "active",
            createdAt);

        await EnsureProjectAsync(
            db,
            Guid.Parse("9fdc54ee-301c-4c68-8a1a-55db3f73110a"),
            member.Id,
            "API Integration",
            "active",
            createdAt);

        await EnsureProjectTaskAsync(
            db,
            Guid.Parse("16b85d0e-4247-4c72-a1df-0c28bd96c463"),
            Guid.Parse("bfb84013-89ce-4536-ac3d-81e7e5099fb5"),
            "Create local database",
            true,
            createdAt);

        await EnsureProjectTaskAsync(
            db,
            Guid.Parse("4bcbeb22-f881-446d-88d5-8a34d4b9633a"),
            Guid.Parse("bfb84013-89ce-4536-ac3d-81e7e5099fb5"),
            "Connect ASP.NET through EF Core",
            false,
            createdAt);

        await EnsureProjectTaskAsync(
            db,
            Guid.Parse("bb831246-8e40-4cc8-9930-0474ff3d041d"),
            Guid.Parse("9fdc54ee-301c-4c68-8a1a-55db3f73110a"),
            "Return database overview endpoint",
            false,
            createdAt);

        await EnsureAnnouncementAsync(
            db,
            Guid.Parse("facfa2a0-b371-42c6-92d9-712d187e8e45"),
            "Local database ready",
            "This announcement is intentionally unrelated to users, projects, or tasks.",
            createdAt);

        await EnsureAnnouncementAsync(
            db,
            Guid.Parse("5b9ecb81-8c59-4b4f-ae87-c9f5d4dcb8ea"),
            "Seed data loaded",
            "Seed rows are inserted by ASP.NET during development startup.",
            createdAt);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

        ThrowIfFailed(result, $"Unable to create role '{roleName}'.");
    }

    private static async Task<AppUser> EnsureDemoUserAsync(
        UserManager<AppUser> userManager,
        Guid id,
        string email,
        string displayName,
        string password,
        string roleName,
        DateTime createdAt)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AppUser
            {
                Id = id,
                Email = email,
                UserName = email,
                DisplayName = displayName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = createdAt
            };

            ThrowIfFailed(await userManager.CreateAsync(user, password), $"Unable to create user '{email}'.");
        }
        else
        {
            user.UserName = email;
            user.Email = email;
            user.DisplayName = displayName;
            user.EmailConfirmed = true;
            user.IsActive = true;

            if (user.CreatedAt == default)
            {
                user.CreatedAt = createdAt;
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
            }

            ThrowIfFailed(await userManager.UpdateAsync(user), $"Unable to update user '{email}'.");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            ThrowIfFailed(await userManager.AddToRoleAsync(user, roleName), $"Unable to assign role '{roleName}'.");
        }

        return user;
    }

    private static async Task EnsureProjectAsync(
        AppDbContext db,
        Guid id,
        Guid ownerId,
        string name,
        string status,
        DateTime createdAt)
    {
        var project = await db.Projects.SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (project is null)
        {
            db.Projects.Add(new Project
            {
                Id = id,
                OwnerId = ownerId,
                Name = name,
                Status = status,
                CreatedAt = createdAt
            });

            return;
        }

        project.OwnerId = ownerId;
        project.Name = name;
        project.Status = status;
    }

    private static async Task EnsureProjectTaskAsync(
        AppDbContext db,
        Guid id,
        Guid projectId,
        string title,
        bool isDone,
        DateTime createdAt)
    {
        var task = await db.ProjectTasks.SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (task is null)
        {
            db.ProjectTasks.Add(new ProjectTask
            {
                Id = id,
                ProjectId = projectId,
                Title = title,
                IsDone = isDone,
                CreatedAt = createdAt
            });

            return;
        }

        task.ProjectId = projectId;
        task.Title = title;
        task.IsDone = isDone;
    }

    private static async Task EnsureAnnouncementAsync(
        AppDbContext db,
        Guid id,
        string title,
        string body,
        DateTime publishedAt)
    {
        var announcement = await db.Announcements.SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (announcement is null)
        {
            db.Announcements.Add(new Announcement
            {
                Id = id,
                Title = title,
                Body = body,
                PublishedAt = publishedAt
            });

            return;
        }

        announcement.Title = title;
        announcement.Body = body;
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));

        throw new InvalidOperationException($"{message} {errors}");
    }
}
