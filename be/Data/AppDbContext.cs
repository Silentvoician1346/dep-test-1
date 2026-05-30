using be.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();

            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.UserName).HasMaxLength(256).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("project");

            entity.HasKey(project => project.Id);

            entity.Property(project => project.Name).HasMaxLength(160).IsRequired();
            entity.Property(project => project.Status).HasMaxLength(50).IsRequired();
            entity.Property(project => project.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity
                .HasOne(project => project.Owner)
                .WithMany(user => user.Projects)
                .HasForeignKey(project => project.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("project_task");

            entity.HasKey(task => task.Id);

            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity
                .HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("announcement");

            entity.HasKey(announcement => announcement.Id);

            entity.Property(announcement => announcement.Title).HasMaxLength(160).IsRequired();
            entity.Property(announcement => announcement.Body).HasMaxLength(2000).IsRequired();
            entity.Property(announcement => announcement.PublishedAt).HasColumnType("timestamp with time zone").IsRequired();
        });
    }
}
