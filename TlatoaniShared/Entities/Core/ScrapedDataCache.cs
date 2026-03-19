using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Core;

/// <summary>
/// Cache de datos scrapeados por partido. Evita re-scrapear en 24h.
/// </summary>
public class ScrapedDataCache
{
    public int Id { get; set; }

    [Required]
    public string MatchName { get; set; } = null!;

    [Required]
    public string SourceName { get; set; } = null!;

    [Required]
    public string Tier { get; set; } = null!;

    /// <summary>Contenido scrapeado (texto)</summary>
    public string? Content { get; set; }

    public bool Success { get; set; }

    public string? Error { get; set; }

    public double ElapsedSeconds { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Expiración del cache (default 24h)</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
