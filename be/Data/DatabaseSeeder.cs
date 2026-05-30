using be.Models;
using be.Services;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.AppUsers.AnyAsync())
        {
            await UpdatePlaceholderPasswordHashesAsync(db);
            return;
        }

        var createdAt = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

        var admin = new AppUser
        {
            Id = Guid.Parse("6c693495-0308-40ee-a61c-cfe7caeecfa4"),
            Email = "admin@example.local",
            DisplayName = "Local Admin",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
            Role = "admin",
            IsActive = true,
            CreatedAt = createdAt
        };

        var member = new AppUser
        {
            Id = Guid.Parse("ec2de4c6-b1e9-4873-a797-c7466d0396b1"),
            Email = "member@example.local",
            DisplayName = "Local Member",
            PasswordHash = PasswordHasher.Hash("Member123!"),
            Role = "member",
            IsActive = true,
            CreatedAt = createdAt
        };

        var onboardingProject = new Project
        {
            Id = Guid.Parse("bfb84013-89ce-4536-ac3d-81e7e5099fb5"),
            Owner = admin,
            Name = "Local Onboarding",
            Status = "active",
            CreatedAt = createdAt
        };

        var apiProject = new Project
        {
            Id = Guid.Parse("9fdc54ee-301c-4c68-8a1a-55db3f73110a"),
            Owner = member,
            Name = "API Integration",
            Status = "active",
            CreatedAt = createdAt
        };

        db.AppUsers.AddRange(admin, member);
        db.Projects.AddRange(onboardingProject, apiProject);
        db.ProjectTasks.AddRange(
            new ProjectTask
            {
                Id = Guid.Parse("16b85d0e-4247-4c72-a1df-0c28bd96c463"),
                Project = onboardingProject,
                Title = "Create local database",
                IsDone = true,
                CreatedAt = createdAt
            },
            new ProjectTask
            {
                Id = Guid.Parse("4bcbeb22-f881-446d-88d5-8a34d4b9633a"),
                Project = onboardingProject,
                Title = "Connect ASP.NET through EF Core",
                IsDone = false,
                CreatedAt = createdAt
            },
            new ProjectTask
            {
                Id = Guid.Parse("bb831246-8e40-4cc8-9930-0474ff3d041d"),
                Project = apiProject,
                Title = "Return database overview endpoint",
                IsDone = false,
                CreatedAt = createdAt
            });

        db.Announcements.AddRange(
            new Announcement
            {
                Id = Guid.Parse("facfa2a0-b371-42c6-92d9-712d187e8e45"),
                Title = "Local database ready",
                Body = "This announcement is intentionally unrelated to users, projects, or tasks.",
                PublishedAt = createdAt
            },
            new Announcement
            {
                Id = Guid.Parse("5b9ecb81-8c59-4b4f-ae87-c9f5d4dcb8ea"),
                Title = "Seed data loaded",
                Body = "Seed rows are inserted by ASP.NET during development startup.",
                PublishedAt = createdAt
            });

        await db.SaveChangesAsync();
    }

    private static async Task UpdatePlaceholderPasswordHashesAsync(AppDbContext db)
    {
        var admin = await db.AppUsers.SingleOrDefaultAsync(user => user.Email == "admin@example.local");
        var member = await db.AppUsers.SingleOrDefaultAsync(user => user.Email == "member@example.local");

        if (admin?.PasswordHash == "local-dev-password-hash-placeholder")
        {
            admin.PasswordHash = PasswordHasher.Hash("Admin123!");
        }

        if (member?.PasswordHash == "local-dev-password-hash-placeholder")
        {
            member.PasswordHash = PasswordHasher.Hash("Member123!");
        }

        await db.SaveChangesAsync();
    }
}
