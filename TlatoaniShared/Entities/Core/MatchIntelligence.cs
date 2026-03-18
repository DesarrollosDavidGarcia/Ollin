using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class MatchIntelligence
{
    public int Id { get; set; }

    [Required]
    public string MatchName { get; set; } = null!;

    [Required]
    public string Jornada { get; set; } = null!;

    [Required]
    public string Phase { get; set; } = null!;

    [Required]
    public string IntelligenceJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
