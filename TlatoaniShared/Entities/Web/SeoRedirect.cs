using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Web;

public class SeoRedirect
{
    public int Id { get; set; }

    [Required]
    public string FromPath { get; set; } = null!;

    [Required]
    public string ToPath { get; set; } = null!;

    public int StatusCode { get; set; } = 301;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
