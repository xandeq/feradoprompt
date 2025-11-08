using Microsoft.EntityFrameworkCore;
using FeraPrompt.Api.Models;

namespace FeraPrompt.Api.Data;

/// <summary>
/// Contexto do banco de dados do sistema Fera do Prompt
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<PromptHistory> PromptHistories => Set<PromptHistory>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configurações de assembly (escalabilidade futura)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configuração da entidade Prompt
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

            // Relacionamento 1:N com PromptHistory
            entity.HasMany(e => e.PromptHistories)
                .WithOne(ph => ph.Prompt)
                .HasForeignKey(ph => ph.PromptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para performance
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Model);
        });

        // Configuração da entidade PromptHistory
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

            // FK já configurada no relacionamento de Prompt
            entity.Property(e => e.PromptId)
                .IsRequired();

            // Índices para performance
            entity.HasIndex(e => e.PromptId);
            entity.HasIndex(e => e.ExecutedAt);
        });

        // Configuração da entidade User
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

            // Índices únicos para Username e Email
            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });
    }
}
