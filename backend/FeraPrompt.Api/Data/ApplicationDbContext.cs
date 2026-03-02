using Microsoft.EntityFrameworkCore;
using FeraPrompt.Api.Models;

namespace FeraPrompt.Api.Data;

/// <summary>
/// Database context for Fera do Prompt.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<PromptHistory> PromptHistories => Set<PromptHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PromptGenerationRecord> PromptGenerationRecords => Set<PromptGenerationRecord>();
    public DbSet<ModelPerformanceStat> ModelPerformanceStats => Set<ModelPerformanceStat>();
    public DbSet<DailyQuotaUsage> DailyQuotaUsages => Set<DailyQuotaUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Body)
                .IsRequired();

            entity.Property(e => e.Model)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.HasMany(e => e.PromptHistories)
                .WithOne(ph => ph.Prompt)
                .HasForeignKey(ph => ph.PromptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Model);
        });

        modelBuilder.Entity<PromptHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Input)
                .IsRequired();

            entity.Property(e => e.Output)
                .IsRequired();

            entity.Property(e => e.ModelUsed)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ExecutedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.PromptId)
                .IsRequired();

            entity.HasIndex(e => e.PromptId);
            entity.HasIndex(e => e.ExecutedAt);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<PromptGenerationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ClientSessionId);
            entity.HasIndex(e => new { e.ParentRecordId, e.Version });
            entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<ModelPerformanceStat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Purpose, e.ModelId }).IsUnique();
            entity.HasIndex(e => e.LastUsedAt);
            entity.Property(e => e.LastUsedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<DailyQuotaUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DateUtc, e.IpAddress }).IsUnique();
            entity.HasIndex(e => e.UpdatedAt);
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
