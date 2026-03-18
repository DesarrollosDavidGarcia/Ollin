using System.ComponentModel.DataAnnotations;
using TlatoaniShared.Entities.Core;

namespace TlatoaniShared.Entities.Web;

public class BlogPost
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = null!;

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [Required]
    public string BodyMarkdown { get; set; } = null!;

    [Required]
    public string BodyHtml { get; set; } = null!;

    [MaxLength(160)]
    public string? MetaDescription { get; set; }

    public string? Keywords { get; set; }

    public string? OgImageUrl { get; set; }

    public int CategoryId { get; set; }
    public BlogCategory Category { get; set; } = null!;

    public ICollection<BlogPostTag> Tags { get; set; } = new List<BlogPostTag>();

    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    [Required]
    public string Status { get; set; } = "draft";

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int ViewCount { get; set; }

    public string? ArticleType { get; set; }

    public int? SourceAnalysisId { get; set; }
    public Analysis? SourceAnalysis { get; set; }
}
