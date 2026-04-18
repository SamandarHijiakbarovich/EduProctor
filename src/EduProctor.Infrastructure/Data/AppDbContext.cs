using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;

namespace EduProctor.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<ExamSession> ExamSessions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<ProctoringEvent> ProctoringEvents { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User - Organization relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Test - Organization relationship
        modelBuilder.Entity<Test>()
            .HasOne(t => t.Organization)
            .WithMany(o => o.Tests)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Slug)
            .IsUnique();

        // Indexes for performance
        modelBuilder.Entity<ExamSession>()
            .HasIndex(e => new { e.TestId, e.UserId });

        modelBuilder.Entity<ProctoringEvent>()
            .HasIndex(p => new { p.SessionId, p.Timestamp });
    }
}