using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ollin.Models;
using TlatoaniShared.Data;

namespace Ollin.Controllers;

public class BlogController : Controller
{
    private readonly TlatoaniDbContext _db;
    private readonly IConfiguration _config;

    public BlogController(TlatoaniDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
    {
        var query = _db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Tags).ThenInclude(t => t.BlogTag)
            .Where(p => p.Status == "published")
            .OrderByDescending(p => p.PublishedAt);

        var totalPosts = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _db.BlogCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        var viewModel = new BlogIndexViewModel
        {
            Posts = posts,
            Categories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            CurrentCategorySlug = null,
            CurrentTagSlug = null
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Post(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var post = await _db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Tags).ThenInclude(t => t.BlogTag)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == "published");

        if (post == null)
            return NotFound();

        post.ViewCount++;
        await _db.SaveChangesAsync();

        var relatedPosts = await _db.BlogPosts
            .Where(p => p.CategoryId == post.CategoryId && p.Id != post.Id && p.Status == "published")
            .OrderByDescending(p => p.PublishedAt)
            .Take(4)
            .ToListAsync();

        var viewModel = new BlogPostViewModel
        {
            Post = post,
            RelatedPosts = relatedPosts
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Category(string slug, int page = 1, int pageSize = 12)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var category = await _db.BlogCategories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (category == null)
            return NotFound();

        var query = _db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Tags).ThenInclude(t => t.BlogTag)
            .Where(p => p.Status == "published" && p.CategoryId == category.Id)
            .OrderByDescending(p => p.PublishedAt);

        var totalPosts = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _db.BlogCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        var viewModel = new BlogIndexViewModel
        {
            Posts = posts,
            Categories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            CurrentCategorySlug = slug,
            CurrentTagSlug = null
        };

        return View("Index", viewModel);
    }

    public async Task<IActionResult> Tag(string slug, int page = 1, int pageSize = 12)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var tag = await _db.BlogTags.FirstOrDefaultAsync(t => t.Slug == slug);
        if (tag == null)
            return NotFound();

        var query = _db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Tags).ThenInclude(t => t.BlogTag)
            .Where(p => p.Status == "published" && p.Tags.Any(t => t.BlogTagId == tag.Id))
            .OrderByDescending(p => p.PublishedAt);

        var totalPosts = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _db.BlogCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        var viewModel = new BlogIndexViewModel
        {
            Posts = posts,
            Categories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            CurrentCategorySlug = null,
            CurrentTagSlug = slug
        };

        return View("Index", viewModel);
    }

    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> Rss()
    {
        var siteUrl = _config["Ollin:SiteUrl"] ?? "https://ollin.mx";
        var siteName = _config["Ollin:SiteName"] ?? "Ollin - Inteligencia Liga MX";

        var posts = await _db.BlogPosts
            .Include(p => p.Category)
            .Where(p => p.Status == "published")
            .OrderByDescending(p => p.PublishedAt)
            .Take(50)
            .ToListAsync();

        var rss = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", siteName),
                    new XElement("link", siteUrl),
                    new XElement("description", "Analisis cuantitativo de Liga MX"),
                    new XElement("language", "es-MX"),
                    new XElement("lastBuildDate", DateTime.UtcNow.ToString("R")),
                    posts.Select(p => new XElement("item",
                        new XElement("title", p.Title),
                        new XElement("link", $"{siteUrl}/blog/{p.Slug}"),
                        new XElement("description", p.Excerpt ?? p.MetaDescription ?? ""),
                        new XElement("pubDate", (p.PublishedAt ?? p.CreatedAt).ToString("R")),
                        new XElement("guid", $"{siteUrl}/blog/{p.Slug}"),
                        new XElement("category", p.Category?.Name ?? "General")
                    ))
                )
            )
        );

        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb))
        {
            rss.Save(writer);
        }

        return Content(sb.ToString(), "application/rss+xml", Encoding.UTF8);
    }
}
