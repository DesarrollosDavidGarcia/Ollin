using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

public class Prediction
{
    public int Id { get; set; }

    [Required]
    public string ExternalId { get; set; } = null!;

    [Required]
    public string Jornada { get; set; } = null!;

    [Required]
    public string MatchName { get; set; } = null!;

    [Required]
    public string Outcome { get; set; } = null!;

    public double ProbModel { get; set; }

    public double BestOdds { get; set; }

    public string BestOddsSource { get; set; } = null!;

    public double Edge { get; set; }

    public double KellyUnits { get; set; }

    public string Confidence { get; set; } = null!;

    public string? ActualResult { get; set; }

    public bool? Won { get; set; }

    public double? PnL { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
