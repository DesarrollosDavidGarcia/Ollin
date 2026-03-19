using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

/// <summary>
/// Partido detectado por OCR o OpenClaw. Persiste para reutilizar datos scrapeados.
/// </summary>
public class ScannedMatch
{
    public int Id { get; set; }

    [Required]
    public string Jornada { get; set; } = null!;

    [Required]
    public string LocalTeam { get; set; } = null!;

    [Required]
    public string AwayTeam { get; set; } = null!;

    [Required]
    public string MatchName { get; set; } = null!;

    /// <summary>Fecha programada del partido (si se conoce)</summary>
    public DateTime? MatchDate { get; set; }

    /// <summary>Origen: "ocr", "openclaw", "manual"</summary>
    [Required]
    public string Source { get; set; } = "openclaw";

    /// <summary>Estado: pending, analyzed, skipped</summary>
    [Required]
    public string Status { get; set; } = "pending";

    /// <summary>ID del Analysis si ya fue analizado</summary>
    public int? AnalysisId { get; set; }

    /// <summary>Tiene datos scrapeados cacheados</summary>
    public bool HasCachedData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AnalyzedAt { get; set; }
}
