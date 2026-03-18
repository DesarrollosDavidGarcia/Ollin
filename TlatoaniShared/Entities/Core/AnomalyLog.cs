using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class AnomalyLog
{
    public int Id { get; set; }

    [Required]
    public string AnomalyType { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public string Severity { get; set; } = null!;

    public string? RelatedMatch { get; set; }

    public string? DataJson { get; set; }

    public DateTime DetectedAt { get; set; }
}
