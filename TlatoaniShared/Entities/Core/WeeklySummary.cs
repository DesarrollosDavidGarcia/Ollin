using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class WeeklySummary
{
    public int Id { get; set; }

    [Required]
    public DateTime WeekStartDate { get; set; }

    [Required]
    public DateTime WeekEndDate { get; set; }

    [Required]
    public string SummaryJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
