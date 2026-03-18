using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.EntityFrameworkCore;
using TlatoaniShared.Data;
using TlatoaniShared.Entities.Web;

namespace Ollin.Services;

public interface IBlogGenerationService { }

public class BlogGenerationService : BackgroundService, IBlogGenerationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BlogGenerationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly MarkdownPipeline _markdownPipeline;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    public BlogGenerationService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<BlogGenerationService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config["NovitaAi:BaseUrl"] ?? "https://api.novita.ai/v3/openai"),
            Timeout = TimeSpan.FromMinutes(5)
        };

        var apiKey = _config["NovitaAi:ApiKey"] ?? "";
        if (!string.IsNullOrEmpty(apiKey))
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlogGenerationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingQueueItems(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BlogGenerationService loop");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingQueueItems(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TlatoaniDbContext>();

        var pendingItems = await db.ContentQueueItems
            .Where(q => q.Status == "pending" && (q.ScheduledFor == null || q.ScheduledFor <= DateTime.UtcNow))
            .OrderBy(q => q.ScheduledFor ?? q.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        if (pendingItems.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} pending content queue items", pendingItems.Count);

        foreach (var item in pendingItems)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                item.Status = "processing";
                await db.SaveChangesAsync(ct);

                var prompt = BuildPrompt(item);
                var generatedContent = await CallNovitaAi(prompt, ct);

                if (string.IsNullOrWhiteSpace(generatedContent))
                {
                    item.Status = "failed";
                    item.ErrorMessage = "Empty response from LLM";
                    item.ProcessedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    continue;
                }

                // Parse generated content
                var (title, body, metaDescription, keywords) = ParseGeneratedContent(generatedContent, item.ArticleType);

                item.GeneratedTitle = title;
                item.GeneratedSlug = GenerateSlug(title);
                item.GeneratedBodyMarkdown = body;
                item.GeneratedMetaDescription = metaDescription;
                item.GeneratedKeywords = keywords;

                // Create blog post
                var htmlBody = Markdown.ToHtml(body, _markdownPipeline);

                // Default category: find or create "General"
                var category = await db.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "general", ct);
                if (category == null)
                {
                    category = new BlogCategory { Name = "General", Slug = "general" };
                    db.BlogCategories.Add(category);
                    await db.SaveChangesAsync(ct);
                }

                var blogPost = new BlogPost
                {
                    Title = title,
                    Slug = item.GeneratedSlug,
                    Excerpt = metaDescription,
                    BodyMarkdown = body,
                    BodyHtml = htmlBody,
                    MetaDescription = metaDescription,
                    Keywords = keywords,
                    CategoryId = category.Id,
                    Status = "published",
                    PublishedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ArticleType = item.ArticleType
                };

                db.BlogPosts.Add(blogPost);
                await db.SaveChangesAsync(ct);

                item.Status = "completed";
                item.ProcessedAt = DateTime.UtcNow;
                item.PublishedBlogPostId = blogPost.Id;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Published blog post '{Title}' (ID: {Id})", blogPost.Title, blogPost.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item {ItemId}", item.Id);
                item.Status = "failed";
                item.ErrorMessage = ex.Message;
                item.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private string BuildPrompt(ContentQueueItem item)
    {
        var sourceData = item.SourceDataJson ?? "{}";

        return item.ArticleType switch
        {
            "preview" => $"""
                Eres un analista experto de Liga MX. Escribe un articulo de preview/pronostico para el siguiente partido.
                Datos del partido: {sourceData}

                El articulo debe incluir:
                - Titulo atractivo y SEO-friendly (maximo 70 caracteres)
                - Analisis de forma reciente de ambos equipos
                - Estadisticas clave (xG, PPDA, set pieces)
                - Historial de enfrentamientos
                - Pronostico fundamentado con datos
                - Meta description (maximo 155 caracteres)
                - Keywords separados por coma

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """,

            "recap" => $"""
                Eres un analista experto de Liga MX. Escribe un articulo de resumen/recap del siguiente partido.
                Datos del partido: {sourceData}

                El articulo debe incluir:
                - Titulo informativo con el resultado
                - Resumen de lo ocurrido
                - Momentos clave del partido
                - Analisis tactico breve
                - Impacto en la tabla general
                - Meta description y keywords

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """,

            "tendencias" => $"""
                Eres un analista cuantitativo de Liga MX. Escribe un articulo sobre tendencias estadisticas actuales.
                Datos: {sourceData}

                El articulo debe incluir:
                - Titulo que refleje la tendencia principal
                - Analisis de datos y graficas textuales
                - Comparativas entre equipos
                - Implicaciones para apuestas de valor
                - Meta description y keywords

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """,

            "estelar" => $"""
                Eres un analista experto de Liga MX. Escribe un articulo sobre el jugador estelar de la jornada.
                Datos: {sourceData}

                El articulo debe incluir:
                - Titulo con el nombre del jugador
                - Estadisticas destacadas de la jornada
                - Contexto de su temporada
                - Comparativa con otros jugadores de su posicion
                - Meta description y keywords

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """,

            "post_jornada" => $"""
                Eres un analista cuantitativo de Liga MX. Escribe un resumen completo de la jornada.
                Datos: {sourceData}

                El articulo debe incluir:
                - Titulo que resuma la jornada
                - Resultados de todos los partidos
                - Sorpresas y confirmaciones
                - Movimientos en la tabla
                - Value bets que se acertaron/fallaron
                - Meta description y keywords

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """,

            _ => $"""
                Eres un analista experto de Liga MX. Escribe un articulo informativo.
                Datos: {sourceData}
                Tipo: {item.ArticleType}

                Formato de respuesta:
                TITULO: [titulo]
                META: [meta description]
                KEYWORDS: [keywords]
                ---
                [cuerpo del articulo en Markdown]
                """
        };
    }

    private async Task<string?> CallNovitaAi(string prompt, CancellationToken ct)
    {
        var model = _config["NovitaAi:Model"] ?? "deepseek/deepseek-v3.2";

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "Eres un periodista deportivo experto en Liga MX con enfoque cuantitativo. Escribes en espanol." },
                new { role = "user", content = prompt }
            },
            max_tokens = 4000,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent;
    }

    private static (string title, string body, string meta, string keywords) ParseGeneratedContent(
        string content, string articleType)
    {
        var title = "Articulo Liga MX";
        var meta = "";
        var keywords = "";
        var body = content;

        // Parse TITULO:
        var titleMatch = Regex.Match(content, @"TITULO:\s*(.+)", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
            title = titleMatch.Groups[1].Value.Trim();

        // Parse META:
        var metaMatch = Regex.Match(content, @"META:\s*(.+)", RegexOptions.IgnoreCase);
        if (metaMatch.Success)
            meta = metaMatch.Groups[1].Value.Trim();

        // Parse KEYWORDS:
        var keywordsMatch = Regex.Match(content, @"KEYWORDS:\s*(.+)", RegexOptions.IgnoreCase);
        if (keywordsMatch.Success)
            keywords = keywordsMatch.Groups[1].Value.Trim();

        // Extract body after ---
        var separatorIndex = content.IndexOf("---", StringComparison.Ordinal);
        if (separatorIndex >= 0)
            body = content[(separatorIndex + 3)..].Trim();

        if (meta.Length > 155)
            meta = meta[..155];

        return (title, body, meta, keywords);
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[aáà]", "a");
        slug = Regex.Replace(slug, @"[eéè]", "e");
        slug = Regex.Replace(slug, @"[iíì]", "i");
        slug = Regex.Replace(slug, @"[oóò]", "o");
        slug = Regex.Replace(slug, @"[uúùü]", "u");
        slug = Regex.Replace(slug, @"ñ", "n");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length > 100)
            slug = slug[..100].TrimEnd('-');

        return slug;
    }
}
