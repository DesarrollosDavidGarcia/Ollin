using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ollin.Models;
using Ollin.Services;

namespace Ollin.Controllers;

[Authorize(Policy = "PremiumOnly")]
public class DashboardController : Controller
{
    private readonly IDashboardDataService _dashboardData;

    public DashboardController(IDashboardDataService dashboardData)
    {
        _dashboardData = dashboardData;
    }

    public async Task<IActionResult> Index()
    {
        var overview = await _dashboardData.GetOverviewAsync();

        var viewModel = new DashboardOverviewViewModel
        {
            TotalPlays = overview.TotalPlays,
            ActivePlaysCount = overview.ActivePlaysCount,
            HitRate = overview.HitRate,
            Roi = overview.Roi,
            RecentPlays = overview.RecentPlays
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Jornada(string jornada)
    {
        if (string.IsNullOrWhiteSpace(jornada))
            return BadRequest();

        var detail = await _dashboardData.GetJornadaAsync(jornada);

        var viewModel = new JornadaViewModel
        {
            JornadaName = detail.JornadaName,
            Matches = detail.Matches
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Match(int id)
    {
        var detail = await _dashboardData.GetMatchDetailAsync(id);
        if (detail == null)
            return NotFound();

        var viewModel = new MatchDetailViewModel
        {
            AnalysisId = detail.AnalysisId,
            MatchName = detail.MatchName,
            Jornada = detail.Jornada,
            AnalysisDate = detail.AnalysisDate,
            EngineResultJson = detail.EngineResultJson,
            EdgeHome = detail.EdgeHome,
            EdgeDraw = detail.EdgeDraw,
            EdgeAway = detail.EdgeAway,
            KillSwitch = detail.KillSwitch,
            Agent4Report = detail.Agent4Report
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Backtesting()
    {
        var data = await _dashboardData.GetBacktestingDataAsync();

        var viewModel = new BacktestingViewModel
        {
            TotalPredictions = data.TotalPredictions,
            ResolvedPredictions = data.ResolvedPredictions,
            HitRate = data.HitRate,
            Roi = data.Roi,
            TotalPnL = data.TotalPnL,
            PnLSeries = data.PnLSeries
        };

        return View(viewModel);
    }
}
