using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TlatoaniShared.Data;
using TlatoaniShared.Entities.Web;

namespace Ollin.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly TlatoaniDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(TlatoaniDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "Admin";
        return View();
    }

    // ===== DB Status =====

    [HttpGet]
    public async Task<IActionResult> DbStatus()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            if (!canConnect)
                return Json(new { success = true, connected = false });

            var applied = (await _db.Database.GetAppliedMigrationsAsync()).ToList();
            var pending = (await _db.Database.GetPendingMigrationsAsync()).ToList();

            int analyses = 0, predictions = 0, summaries = 0, scanned = 0, cache = 0, users = 0;
            try { analyses = await _db.Analyses.CountAsync(); } catch { }
            try { predictions = await _db.Predictions.CountAsync(); } catch { }
            try { summaries = await _db.JornadaSummaries.CountAsync(); } catch { }
            try { scanned = await _db.ScannedMatches.CountAsync(); } catch { }
            try { cache = await _db.ScrapedDataCaches.CountAsync(); } catch { }
            try { users = await _userManager.Users.CountAsync(); } catch { }

            return Json(new
            {
                success = true,
                connected = true,
                provider = _db.Database.ProviderName,
                appliedMigrations = applied,
                pendingMigrations = pending,
                tables = new { analyses, predictions, summaries, scanned, cache, users }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ===== Migrations =====

    [HttpPost]
    public async Task<IActionResult> ApplyMigrations()
    {
        try
        {
            var pending = (await _db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
                return Json(new { success = true, message = "No hay migraciones pendientes", applied = 0 });

            await _db.Database.MigrateAsync();
            return Json(new { success = true, message = $"{pending.Count} migracion(es) aplicada(s)", applied = pending.Count, migrations = pending });
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {
            try
            {
                var pending2 = (await _db.Database.GetPendingMigrationsAsync()).ToList();
                foreach (var m in pending2)
                    await _db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1}) ON CONFLICT DO NOTHING", m, "9.0.4");
                return Json(new { success = true, message = $"Tablas existian. {pending2.Count} marcada(s) como aplicadas." });
            }
            catch (Exception inner)
            {
                return Json(new { success = false, error = inner.Message });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ===== Table Management =====

    [HttpPost]
    public async Task<IActionResult> ClearTable(string table)
    {
        try
        {
            int count = 0;
            switch (table?.ToLower())
            {
                case "analyses":
                    count = await _db.Analyses.CountAsync();
                    _db.Analyses.RemoveRange(await _db.Analyses.ToListAsync());
                    break;
                case "predictions":
                    count = await _db.Predictions.CountAsync();
                    _db.Predictions.RemoveRange(await _db.Predictions.ToListAsync());
                    break;
                case "summaries":
                    count = await _db.JornadaSummaries.CountAsync();
                    _db.JornadaSummaries.RemoveRange(await _db.JornadaSummaries.ToListAsync());
                    break;
                case "scanned":
                    count = await _db.ScannedMatches.CountAsync();
                    _db.ScannedMatches.RemoveRange(await _db.ScannedMatches.ToListAsync());
                    break;
                case "cache":
                    count = await _db.ScrapedDataCaches.CountAsync();
                    _db.ScrapedDataCaches.RemoveRange(await _db.ScrapedDataCaches.ToListAsync());
                    break;
                default:
                    return Json(new { success = false, error = $"Tabla '{table}' no reconocida" });
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"{count} registros eliminados de {table}" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ClearAllData()
    {
        try
        {
            int a = 0, p = 0, s = 0, sc = 0, c = 0;
            try { a = await _db.Analyses.CountAsync(); _db.Analyses.RemoveRange(await _db.Analyses.ToListAsync()); } catch { }
            try { p = await _db.Predictions.CountAsync(); _db.Predictions.RemoveRange(await _db.Predictions.ToListAsync()); } catch { }
            try { s = await _db.JornadaSummaries.CountAsync(); _db.JornadaSummaries.RemoveRange(await _db.JornadaSummaries.ToListAsync()); } catch { }
            try { sc = await _db.ScannedMatches.CountAsync(); _db.ScannedMatches.RemoveRange(await _db.ScannedMatches.ToListAsync()); } catch { }
            try { c = await _db.ScrapedDataCaches.CountAsync(); _db.ScrapedDataCaches.RemoveRange(await _db.ScrapedDataCaches.ToListAsync()); } catch { }
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Eliminados: {a} analisis, {p} predicciones, {s} resumenes, {sc} partidos, {c} cache" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ===== User Management =====

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.DisplayName,
                    u.SubscriptionTier,
                    roles = roles.ToList(),
                    u.CreatedAt
                });
            }
            return Json(new { success = true, users = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ===== CSV Export =====

    [HttpGet]
    public async Task<IActionResult> ExportDayCsv(string date)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return BadRequest("Formato: yyyy-MM-dd");

        var dayEnd = day.AddDays(1);
        var analyses = await _db.Analyses
            .Where(a => a.AnalysisDate >= day && a.AnalysisDate < dayEnd)
            .OrderBy(a => a.AnalysisDate)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Jornada,Partido,Fecha,Hora,ScoreLocal,ScoreVisitante,ProbLocal,ProbEmpate,ProbVisitante,EdgeLocal,EdgeEmpate,EdgeVisitante,PlaysActivos,KillSwitch");
        foreach (var a in analyses)
        {
            static string D(double? v) => v.HasValue ? v.Value.ToString("F4", CultureInfo.InvariantCulture) : "";
            static string Pct(double? v) => v.HasValue ? (v.Value * 100).ToString("F1", CultureInfo.InvariantCulture) + "%" : "";
            static string Esc(string? v) => v == null ? "" : $"\"{v.Replace("\"", "\"\"")}\"";
            sb.AppendLine($"{a.Id},{Esc(a.Jornada)},{Esc(a.MatchName)},{a.AnalysisDate:yyyy-MM-dd},{a.AnalysisDate:HH:mm},{D(a.LocalScore)},{D(a.VisitanteScore)},{Pct(a.ProbModelHome)},{Pct(a.ProbModelDraw)},{Pct(a.ProbModelAway)},{Pct(a.EdgeHome)},{Pct(a.EdgeDraw)},{Pct(a.EdgeAway)},{a.HasActivePlays},{a.KillSwitch}");
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"analisis_{date}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportWeekCsv(string? weekStart)
    {
        DateTime start;
        if (!string.IsNullOrEmpty(weekStart) && DateTime.TryParseExact(weekStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            start = parsed;
        else
        {
            var today = DateTime.UtcNow.Date;
            int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            start = today.AddDays(-diff);
        }

        var end = start.AddDays(7);
        var analyses = await _db.Analyses
            .Where(a => a.AnalysisDate >= start && a.AnalysisDate < end)
            .OrderBy(a => a.AnalysisDate)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Jornada,Partido,Fecha,Hora,ScoreLocal,ScoreVisitante,ProbLocal,ProbEmpate,ProbVisitante,EdgeLocal,EdgeEmpate,EdgeVisitante,PlaysActivos,KillSwitch");
        foreach (var a in analyses)
        {
            static string D(double? v) => v.HasValue ? v.Value.ToString("F4", CultureInfo.InvariantCulture) : "";
            static string Pct(double? v) => v.HasValue ? (v.Value * 100).ToString("F1", CultureInfo.InvariantCulture) + "%" : "";
            static string Esc(string? v) => v == null ? "" : $"\"{v.Replace("\"", "\"\"")}\"";
            sb.AppendLine($"{a.Id},{Esc(a.Jornada)},{Esc(a.MatchName)},{a.AnalysisDate:yyyy-MM-dd},{a.AnalysisDate:HH:mm},{D(a.LocalScore)},{D(a.VisitanteScore)},{Pct(a.ProbModelHome)},{Pct(a.ProbModelDraw)},{Pct(a.ProbModelAway)},{Pct(a.EdgeHome)},{Pct(a.EdgeDraw)},{Pct(a.EdgeAway)},{a.HasActivePlays},{a.KillSwitch}");
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"analisis_semana_{start:yyyyMMdd}.csv");
    }
}
