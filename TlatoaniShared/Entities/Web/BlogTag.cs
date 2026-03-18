using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Web;

public class BlogTag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = null!;

    public ICollection<BlogPostTag> PostTags { get; set; } = new List<BlogPostTag>();
}
