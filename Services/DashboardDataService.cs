using Microsoft.EntityFrameworkCore;
using TlatoaniShared.Data;

namespace Ollin.Services;

// ── DTOs ────────────────────────────────────────────────────────────────────

public record DashboardOverview(
    int TotalPlays,
    int ActivePlaysCount,
    double HitRate,
    double Roi,
    List<RecentPlay> RecentPlays);

public record RecentPlay(
    int PredictionId,
    string MatchName,
    string Jornada,
    string Outcome,
    double Edge,
    double BestOdds,
    string BestOddsSource,
    double KellyUnits,
    string Confidence,
    bool? Won,
    double? PnL,
    DateTime CreatedAt);

public record JornadaDetail(
    string JornadaName,
    List<JornadaMatch> Matches);

public record JornadaMatch(
    int AnalysisId,
    string MatchName,
    double? EdgeHome,
    double? EdgeDraw,
    double? EdgeAway,
    bool HasActivePlays,
    bool KillSwitch,
    string TrafficLight);

public record MatchDetail(
    int AnalysisId,
    string MatchName,
    string Jornada,
    DateTime AnalysisDate,
    string? EngineResultJson,
    double? EdgeHome,
    double? EdgeDraw,
    double? EdgeAway,
    bool KillSwitch,
    string? Agent4Report);

public record BacktestingData(
    int TotalPredictions,
    int ResolvedPredictions,
    double HitRate,
    double Roi,
    double TotalPnL,
    List<PnLDataPoint> PnLSeries);

public record PnLDataPoint(
    DateTime Date,
    double CumulativePnL,
    double DailyPnL);

// ── Interface ───────────────────────────────────────────────────────────────

public interface IDashboardDataService
{
    Task<DashboardOverview> GetOverviewAsync();
    Task<JornadaDetail> GetJornadaAsync(string jornada);
    Task<MatchDetail?> GetMatchDetailAsync(int analysisId);
    Task<BacktestingData> GetBacktestingDataAsync();
}

// ── Implementation ──────────────────────────────────────────────────────────

public class DashboardDataService : IDashboardDataService
{
    private readonly TlatoaniDbContext _db;

    public DashboardDataService(TlatoaniDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardOverview> GetOverviewAsync()
    {
        var predictions = await _db.Predictions.ToListAsync();

        var totalPlays = predictions.Count;
        var resolved = predictions.Where(p => p.Won.HasValue).ToList();
        var activePlays = predictions.Count(p => !p.Won.HasValue && p.ActualResult == null);
        var hitRate = resolved.Count > 0
            ? resolved.Count(p => p.Won == true) / (double)resolved.Count * 100.0
            : 0.0;
        var totalStaked = resolved.Sum(p => p.KellyUnits);
        var totalPnL = resolved.Sum(p => p.PnL ?? 0.0);
        var roi = totalStaked > 0 ? totalPnL / totalStaked * 100.0 : 0.0;

        var recentPlays = predictions
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .Select(p => new RecentPlay(
                p.Id,
                p.MatchName,
                p.Jornada,
                p.Outcome,
                p.Edge,
                p.BestOdds,
                p.BestOddsSource,
                p.KellyUnits,
                p.Confidence,
                p.Won,
                p.PnL,
                p.CreatedAt))
            .ToList();

        return new DashboardOverview(totalPlays, activePlays, hitRate, roi, recentPlays);
    }

    public async Task<JornadaDetail> GetJornadaAsync(string jornada)
    {
        var analyses = await _db.Analyses
            .Where(a => a.Jornada == jornada)
            .OrderBy(a => a.AnalysisDate)
            .ToListAsync();

        var matches = analyses.Select(a =>
        {
            var trafficLight = "gray";
            if (a.KillSwitch)
                trafficLight = "red";
            else if (a.HasActivePlays)
            {
                var maxEdge = new[] { a.EdgeHome ?? 0, a.EdgeDraw ?? 0, a.EdgeAway ?? 0 }.Max();
                trafficLight = maxEdge >= 10 ? "green" : maxEdge >= 5 ? "yellow" : "gray";
            }

            return new JornadaMatch(
                a.Id,
                a.MatchName,
                a.EdgeHome,
                a.EdgeDraw,
                a.EdgeAway,
                a.HasActivePlays,
                a.KillSwitch,
                trafficLight);
        }).ToList();

        return new JornadaDetail(jornada, matches);
    }

    public async Task<MatchDetail?> GetMatchDetailAsync(int analysisId)
    {
        var analysis = await _db.Analyses.FindAsync(analysisId);
        if (analysis == null)
            return null;

        return new MatchDetail(
            analysis.Id,
            analysis.MatchName,
            analysis.Jornada,
            analysis.AnalysisDate,
            analysis.EngineResultJson,
            analysis.EdgeHome,
            analysis.EdgeDraw,
            analysis.EdgeAway,
            analysis.KillSwitch,
            analysis.Agent4Response);
    }

    public async Task<BacktestingData> GetBacktestingDataAsync()
    {
        var predictions = await _db.Predictions
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var totalPredictions = predictions.Count;
        var resolved = predictions.Where(p => p.Won.HasValue).ToList();
        var resolvedCount = resolved.Count;
        var hitRate = resolvedCount > 0
            ? resolved.Count(p => p.Won == true) / (double)resolvedCount * 100.0
            : 0.0;
        var totalStaked = resolved.Sum(p => p.KellyUnits);
        var totalPnL = resolved.Sum(p => p.PnL ?? 0.0);
        var roi = totalStaked > 0 ? totalPnL / totalStaked * 100.0 : 0.0;

        // Build cumulative PnL series
        var pnlSeries = new List<PnLDataPoint>();
        double cumulative = 0;

        var resolvedByDate = resolved
            .Where(p => p.ResolvedAt.HasValue)
            .GroupBy(p => p.ResolvedAt!.Value.Date)
            .OrderBy(g => g.Key);

        foreach (var group in resolvedByDate)
        {
            var dailyPnL = group.Sum(p => p.PnL ?? 0.0);
            cumulative += dailyPnL;
            pnlSeries.Add(new PnLDataPoint(group.Key, cumulative, dailyPnL));
        }

        return new BacktestingData(totalPredictions, resolvedCount, hitRate, roi, totalPnL, pnlSeries);
    }
}
