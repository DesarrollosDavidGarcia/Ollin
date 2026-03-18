using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class DailyScoutLog
{
    public int Id { get; set; }

    [Required]
    public DateTime ScoutDate { get; set; }

    [Required]
    public string TeamName { get; set; } = null!;

    [Required]
    public string DataJson { get; set; } = null!;

    [Required]
    public string Source { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
