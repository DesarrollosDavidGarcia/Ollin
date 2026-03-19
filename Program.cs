using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ollin.Services;
using TlatoaniShared.Data;
using TlatoaniShared.Entities.Web;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TlatoaniDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TlatoaniDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// ── Authorization ───────────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PremiumOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("SubscriptionTier", "Premium") ||
            context.User.IsInRole("Admin")));
});

// ── Application Services ────────────────────────────────────────────────────
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IDashboardDataService, DashboardDataService>();
builder.Services.AddSingleton<ISeoService, SeoService>();
builder.Services.AddSingleton<IBlogGenerationService, BlogGenerationService>();
builder.Services.AddHostedService(sp => (BlogGenerationService)sp.GetRequiredService<IBlogGenerationService>());

// ── MVC ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Seed Admin User ─────────────────────────────────────────────────────────
using (var seedScope = app.Services.CreateScope())
{
    var sp = seedScope.ServiceProvider;
    try
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var adminEmail = "admin@ollin.mx";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Administrador",
                SubscriptionTier = "Premium",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Prueba@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Admin seed failed (DB may not be ready yet)");
    }
}

// ── Middleware Pipeline ─────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

// SEO: redirect trailing slashes (e.g., /blog/ → /blog)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != "/" && path?.EndsWith("/") == true)
    {
        var query = context.Request.QueryString;
        context.Response.StatusCode = 301;
        context.Response.Headers.Location = path.TrimEnd('/') + query;
        return;
    }
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ──────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "blog-post",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Post" });

app.MapControllerRoute(
    name: "blog-category",
    pattern: "blog/categoria/{slug}",
    defaults: new { controller = "Blog", action = "Category" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
