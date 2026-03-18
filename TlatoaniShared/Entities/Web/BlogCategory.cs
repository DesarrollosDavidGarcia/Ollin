using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Web;

public class BlogCategory
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}
