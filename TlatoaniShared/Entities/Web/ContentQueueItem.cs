using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Web;

public class ContentQueueItem
{
    public int Id { get; set; }

    [Required]
    public string ArticleType { get; set; } = null!;

    public string? SourceDataJson { get; set; }

    public string? PromptUsed { get; set; }

    [Required]
    public string Status { get; set; } = "pending";

    public string? GeneratedTitle { get; set; }

    public string? GeneratedSlug { get; set; }

    public string? GeneratedBodyMarkdown { get; set; }

    public string? GeneratedMetaDescription { get; set; }

    public string? GeneratedKeywords { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ScheduledFor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int? PublishedBlogPostId { get; set; }
    public BlogPost? PublishedBlogPost { get; set; }
}
