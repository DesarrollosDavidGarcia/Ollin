using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TlatoaniShared.Data;

namespace Ollin.Controllers;

public class SitemapController : Controller
{
    private readonly TlatoaniDbContext _db;
    private readonly IConfiguration _config;

    public SitemapController(TlatoaniDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Index()
    {
        var siteUrl = _config["Ollin:SiteUrl"] ?? "https://ollin.mx";
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urls = new List<XElement>();

        // Static pages
        urls.Add(new XElement(ns + "url",
            new XElement(ns + "loc", siteUrl),
            new XElement(ns + "changefreq", "daily"),
            new XElement(ns + "priority", "1.0")));

        urls.Add(new XElement(ns + "url",
            new XElement(ns + "loc", $"{siteUrl}/blog"),
            new XElement(ns + "changefreq", "daily"),
            new XElement(ns + "priority", "0.9")));

        // Blog posts
        var posts = await _db.BlogPosts
            .Where(p => p.Status == "published")
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new { p.Slug, p.PublishedAt, p.UpdatedAt })
            .ToListAsync();

        foreach (var post in posts)
        {
            var lastMod = post.UpdatedAt ?? post.PublishedAt ?? DateTime.UtcNow;
            urls.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{siteUrl}/blog/{post.Slug}"),
                new XElement(ns + "lastmod", lastMod.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "0.8")));
        }

        // Blog categories
        var categories = await _db.BlogCategories
            .Select(c => c.Slug)
            .ToListAsync();

        foreach (var catSlug in categories)
        {
            urls.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{siteUrl}/blog/categoria/{catSlug}"),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "0.6")));
        }

        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset", urls));

        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb))
        {
            sitemap.Save(writer);
        }

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
