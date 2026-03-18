using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class PlayerSeasonStats
{
    public int Id { get; set; }

    [Required]
    public string PlayerName { get; set; } = null!;

    [Required]
    public string TeamName { get; set; } = null!;

    [Required]
    public string Season { get; set; } = null!;

    [Required]
    public string StatsJson { get; set; } = null!;

    public DateTime LastUpdated { get; set; }
}
