using be.Models;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_user");

            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();

            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(50).IsRequired();
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
