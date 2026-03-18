namespace TlatoaniShared.Entities.Web;

public class BlogPostTag
{
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;

    public int BlogTagId { get; set; }
    public BlogTag BlogTag { get; set; } = null!;
}
