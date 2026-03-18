using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TlatoaniShared.Entities.Core;

public class Analysis
{
    public int Id { get; set; }

    [Required]
    public string Jornada { get; set; } = null!;

    [Required]
    public string MatchName { get; set; } = null!;

    public DateTime AnalysisDate { get; set; }

    public string? OcrRawText { get; set; }

    [Column("agent1_response")]
    public string? Agent1Response { get; set; }

    [Column("agent2_response")]
    public string? Agent2Response { get; set; }

    [Column("agent3_response")]
    public string? Agent3Response { get; set; }

    [Column("agent4_response")]
    public string? Agent4Response { get; set; }

    public string? EngineResultJson { get; set; }

    public double? LocalScore { get; set; }

    public double? VisitanteScore { get; set; }

    public double? ProbModelHome { get; set; }

    public double? ProbModelDraw { get; set; }

    public double? ProbModelAway { get; set; }

    public double? EdgeHome { get; set; }

    public double? EdgeDraw { get; set; }

    public double? EdgeAway { get; set; }

    public bool HasActivePlays { get; set; }

    public bool KillSwitch { get; set; }

    public double TotalElapsedSeconds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
