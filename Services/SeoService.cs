using System.Text.Json;
using TlatoaniShared.Entities.Web;

namespace Ollin.Services;

public interface ISeoService
{
    string GetArticleJsonLd(BlogPost post, string siteUrl);
    string GetBreadcrumbJsonLd(List<(string name, string url)> crumbs);
    string GetSportsEventJsonLd(string homeTeam, string awayTeam, DateTime date);
}

public class SeoService : ISeoService
{
    public string GetArticleJsonLd(BlogPost post, string siteUrl)
    {
        var jsonLd = new
        {
            @context = "https://schema.org",
            @type = "Article",
            headline = post.Title,
            description = post.MetaDescription ?? post.Excerpt ?? "",
            image = post.OgImageUrl ?? $"{siteUrl}/images/og-default.jpg",
            author = new
            {
                @type = "Organization",
                name = "Ollin"
            },
            publisher = new
            {
                @type = "Organization",
                name = "Ollin - Inteligencia Liga MX",
                logo = new
                {
                    @type = "ImageObject",
                    url = $"{siteUrl}/images/logo.png"
                }
            },
            datePublished = (post.PublishedAt ?? post.CreatedAt).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            dateModified = (post.UpdatedAt ?? post.PublishedAt ?? post.CreatedAt).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            mainEntityOfPage = new
            {
                @type = "WebPage",
                @id = $"{siteUrl}/blog/{post.Slug}"
            },
            keywords = post.Keywords ?? "",
            articleSection = post.Category?.Name ?? "Liga MX"
        };

        return SerializeJsonLd(jsonLd);
    }

    public string GetBreadcrumbJsonLd(List<(string name, string url)> crumbs)
    {
        var items = crumbs.Select((crumb, index) => new
        {
            @type = "ListItem",
            position = index + 1,
            name = crumb.name,
            item = crumb.url
        }).ToArray();

        var jsonLd = new
        {
            @context = "https://schema.org",
            @type = "BreadcrumbList",
            itemListElement = items
        };

        return SerializeJsonLd(jsonLd);
    }

    public string GetSportsEventJsonLd(string homeTeam, string awayTeam, DateTime date)
    {
        var jsonLd = new
        {
            @context = "https://schema.org",
            @type = "SportsEvent",
            name = $"{homeTeam} vs {awayTeam}",
            startDate = date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            homeTeam = new
            {
                @type = "SportsTeam",
                name = homeTeam
            },
            awayTeam = new
            {
                @type = "SportsTeam",
                name = awayTeam
            },
            sport = "Soccer",
            eventAttendanceMode = "https://schema.org/OfflineEventAttendanceMode"
        };

        return SerializeJsonLd(jsonLd);
    }

    private static string SerializeJsonLd(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return $"<script type=\"application/ld+json\">\n{json}\n</script>";
    }
}
