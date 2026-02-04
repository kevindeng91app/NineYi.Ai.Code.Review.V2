using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Infrastructure.Data;

public class CodeReviewDbContext : DbContext
{
    public CodeReviewDbContext(DbContextOptions<CodeReviewDbContext> options) : base(options)
    {
    }

    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RepositoryRuleMapping> RepositoryRuleMappings => Set<RepositoryRuleMapping>();
    public DbSet<ReviewLog> ReviewLogs => Set<ReviewLog>();
    public DbSet<ReviewFileLog> ReviewFileLogs => Set<ReviewFileLog>();
    public DbSet<RuleStatistics> RuleStatistics => Set<RuleStatistics>();
    public DbSet<HotKeyword> HotKeywords => Set<HotKeyword>();
    public DbSet<DifyUsageLog> DifyUsageLogs => Set<DifyUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Repository
        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Platform, e.PlatformRepositoryId }).IsUnique();
            entity.HasIndex(e => new { e.Platform, e.FullName }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PlatformRepositoryId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.WebhookSecret).HasMaxLength(200);
            entity.Property(e => e.ApiBaseUrl).HasMaxLength(500);
        });

        // Rule
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.DifyApiEndpoint).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DifyApiKey).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FilePatterns).HasMaxLength(500);
        });

        // RepositoryRuleMapping
        modelBuilder.Entity<RepositoryRuleMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RepositoryId, e.RuleId }).IsUnique();
            entity.Property(e => e.FilePatternsOverride).HasMaxLength(500);

            entity.HasOne(e => e.Repository)
                .WithMany(r => r.RuleMappings)
                .HasForeignKey(e => e.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Rule)
                .WithMany(r => r.RepositoryMappings)
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReviewLog
        modelBuilder.Entity<ReviewLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RepositoryId, e.PullRequestNumber });
            entity.HasIndex(e => e.StartedAt);
            entity.Property(e => e.PullRequestTitle).HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(e => e.Repository)
                .WithMany(r => r.ReviewLogs)
                .HasForeignKey(e => e.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReviewFileLog
        modelBuilder.Entity<ReviewFileLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReviewLogId);
            entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.MatchedKeywords).HasMaxLength(1000);

            entity.HasOne(e => e.ReviewLog)
                .WithMany(r => r.FileLogs)
                .HasForeignKey(e => e.ReviewLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RuleStatistics
        modelBuilder.Entity<RuleStatistics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RuleId, e.StatDate }).IsUnique();
            entity.HasIndex(e => e.StatDate);

            entity.HasOne(e => e.Rule)
                .WithMany(r => r.Statistics)
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HotKeyword
        modelBuilder.Entity<HotKeyword>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Keyword);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Keyword).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AlertMessage).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.FilePatterns).HasMaxLength(500);
        });

        // DifyUsageLog
        modelBuilder.Entity<DifyUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReviewLogId);
            entity.HasIndex(e => e.RuleId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.DifyRequestId).HasMaxLength(200);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasOne(e => e.ReviewLog)
                .WithMany()
                .HasForeignKey(e => e.ReviewLogId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Rule)
                .WithMany()
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
