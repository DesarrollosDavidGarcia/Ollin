using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class JornadaSummary
{
    public int Id { get; set; }

    [Required]
    public string Jornada { get; set; } = null!;

    public int TotalMatches { get; set; }

    public int ActivePlays { get; set; }

    public double TotalCapitalPercent { get; set; }

    public double ExpectedReturn { get; set; }

    public bool KillSwitchGlobal { get; set; }

    public string? PlaysJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
