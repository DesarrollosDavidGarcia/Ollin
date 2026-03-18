using System.ComponentModel.DataAnnotations;
using Ollin.Services;
using TlatoaniShared.Entities.Web;

namespace Ollin.Models;

// ── Landing ─────────────────────────────────────────────────────────────────

public class LandingViewModel
{
    public List<FeatureItem> Features { get; set; } = new();
    public List<TestimonialItem> Testimonials { get; set; } = new();
    public decimal MonthlyPrice { get; set; } = 299;
}

public class FeatureItem
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
}

public class TestimonialItem
{
    public string Name { get; set; } = null!;
    public string Quote { get; set; } = null!;
    public string Role { get; set; } = null!;
}

// ── Blog ────────────────────────────────────────────────────────────────────

public class BlogIndexViewModel
{
    public List<BlogPost> Posts { get; set; } = new();
    public List<BlogCategory> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? CurrentCategorySlug { get; set; }
    public string? CurrentTagSlug { get; set; }
}

public class BlogPostViewModel
{
    public BlogPost Post { get; set; } = null!;
    public List<BlogPost> RelatedPosts { get; set; } = new();
    public string? ArticleJsonLd { get; set; }
    public string? BreadcrumbJsonLd { get; set; }
    public List<(string Name, string Url)> Breadcrumbs { get; set; } = new();
}

// ── Dashboard ───────────────────────────────────────────────────────────────

public class DashboardOverviewViewModel
{
    public int TotalPlays { get; set; }
    public int ActivePlaysCount { get; set; }
    public double HitRate { get; set; }
    public double Roi { get; set; }
    public List<RecentPlay> RecentPlays { get; set; } = new();
}

public class JornadaViewModel
{
    public string JornadaName { get; set; } = null!;
    public List<JornadaMatch> Matches { get; set; } = new();
}

public class MatchDetailViewModel
{
    public int AnalysisId { get; set; }
    public string MatchName { get; set; } = null!;
    public string Jornada { get; set; } = null!;
    public DateTime AnalysisDate { get; set; }
    public string? EngineResultJson { get; set; }
    public double? EdgeHome { get; set; }
    public double? EdgeDraw { get; set; }
    public double? EdgeAway { get; set; }
    public bool KillSwitch { get; set; }
    public string? Agent4Report { get; set; }
}

public class BacktestingViewModel
{
    public int TotalPredictions { get; set; }
    public int ResolvedPredictions { get; set; }
    public double HitRate { get; set; }
    public double Roi { get; set; }
    public double TotalPnL { get; set; }
    public List<PnLDataPoint> PnLSeries { get; set; } = new();
}

// ── Account ─────────────────────────────────────────────────────────────────

public class LoginViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email invalido")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es requerida")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email invalido")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es requerida")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Confirme la contrasena")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contrasenas no coinciden")]
    [Display(Name = "Confirmar contrasena")]
    public string ConfirmPassword { get; set; } = null!;

    [MaxLength(100)]
    [Display(Name = "Nombre")]
    public string? DisplayName { get; set; }
}
