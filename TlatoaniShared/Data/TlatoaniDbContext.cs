using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TlatoaniShared.Entities.Core;
using TlatoaniShared.Entities.Web;

namespace TlatoaniShared.Data;

public class TlatoaniDbContext : IdentityDbContext<ApplicationUser>
{
    public TlatoaniDbContext(DbContextOptions<TlatoaniDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<JornadaSummary> JornadaSummaries => Set<JornadaSummary>();
    public DbSet<MatchIntelligence> MatchIntelligences => Set<MatchIntelligence>();
    public DbSet<DailyScoutLog> DailyScoutLogs => Set<DailyScoutLog>();
    public DbSet<WeeklySummary> WeeklySummaries => Set<WeeklySummary>();
    public DbSet<TeamSeasonStats> TeamSeasonStats => Set<TeamSeasonStats>();
    public DbSet<PlayerSeasonStats> PlayerSeasonStats => Set<PlayerSeasonStats>();
    public DbSet<AnomalyLog> AnomalyLogs => Set<AnomalyLog>();
    public DbSet<ScannedMatch> ScannedMatches => Set<ScannedMatch>();
    public DbSet<ScrapedDataCache> ScrapedDataCaches => Set<ScrapedDataCache>();

    // Web entities
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogTag> BlogTags => Set<BlogTag>();
    public DbSet<BlogPostTag> BlogPostTags => Set<BlogPostTag>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ContentQueueItem> ContentQueueItems => Set<ContentQueueItem>();
    public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Core schema ──────────────────────────────────────────────

        builder.Entity<Analysis>(entity =>
        {
            entity.ToTable("analyses", "core");
        });

        builder.Entity<Prediction>(entity =>
        {
            entity.ToTable("predictions", "core");
            entity.HasIndex(e => e.ExternalId).IsUnique();
        });

        builder.Entity<JornadaSummary>(entity =>
        {
            entity.ToTable("jornada_summaries", "core");
        });

        builder.Entity<MatchIntelligence>(entity =>
        {
            entity.ToTable("match_intelligences", "core");
        });

        builder.Entity<DailyScoutLog>(entity =>
        {
            entity.ToTable("daily_scout_logs", "core");
        });

        builder.Entity<WeeklySummary>(entity =>
        {
            entity.ToTable("weekly_summaries", "core");
        });

        builder.Entity<TeamSeasonStats>(entity =>
        {
            entity.ToTable("team_season_stats", "core");
        });

        builder.Entity<PlayerSeasonStats>(entity =>
        {
            entity.ToTable("player_season_stats", "core");
        });

        builder.Entity<AnomalyLog>(entity =>
        {
            entity.ToTable("anomaly_logs", "core");
        });

        builder.Entity<ScannedMatch>(entity =>
        {
            entity.ToTable("scanned_matches", "core");
            entity.HasIndex(e => new { e.Jornada, e.MatchName }).IsUnique();
        });

        builder.Entity<ScrapedDataCache>(entity =>
        {
            entity.ToTable("scraped_data_cache", "core");
            entity.HasIndex(e => new { e.MatchName, e.SourceName });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // ── Web schema ───────────────────────────────────────────────

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users", "web");
        });

        builder.Entity<BlogPost>(entity =>
        {
            entity.ToTable("blog_posts", "web");
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SourceAnalysis)
                .WithMany()
                .HasForeignKey(e => e.SourceAnalysisId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BlogCategory>(entity =>
        {
            entity.ToTable("blog_categories", "web");
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        builder.Entity<BlogTag>(entity =>
        {
            entity.ToTable("blog_tags", "web");
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        builder.Entity<BlogPostTag>(entity =>
        {
            entity.ToTable("blog_post_tags", "web");
            entity.HasKey(e => new { e.BlogPostId, e.BlogTagId });

            entity.HasOne(e => e.BlogPost)
                .WithMany(p => p.Tags)
                .HasForeignKey(e => e.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BlogTag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(e => e.BlogTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions", "web");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContentQueueItem>(entity =>
        {
            entity.ToTable("content_queue_items", "web");

            entity.HasOne(e => e.PublishedBlogPost)
                .WithMany()
                .HasForeignKey(e => e.PublishedBlogPostId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SeoRedirect>(entity =>
        {
            entity.ToTable("seo_redirects", "web");
            entity.HasIndex(e => e.FromPath).IsUnique();
        });

        // Identity tables in web schema
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(entity =>
        {
            entity.ToTable("roles", "web");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("user_roles", "web");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("user_claims", "web");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("user_logins", "web");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("role_claims", "web");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("user_tokens", "web");
        });
    }
}
