using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ollin.Services;
using TlatoaniShared.Data;
using TlatoaniShared.Entities.Web;

namespace Ollin.Controllers;

public class SubscriptionController : Controller
{
    private readonly IStripeService _stripeService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TlatoaniDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IStripeService stripeService,
        UserManager<ApplicationUser> userManager,
        TlatoaniDbContext db,
        IConfiguration config,
        ILogger<SubscriptionController> logger)
    {
        _stripeService = stripeService;
        _userManager = userManager;
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(user.Id, user.Email!);
            return Redirect(sessionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for user {UserId}", user.Id);
            TempData["Error"] = "Error al iniciar el proceso de pago. Intente de nuevo.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public IActionResult Success(string session_id)
    {
        ViewData["SessionId"] = session_id;
        return View();
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? "";

        try
        {
            await _stripeService.HandleWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var subscription = await _db.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription?.StripeCustomerId == null)
        {
            TempData["Error"] = "No se encontro una suscripcion activa.";
            return RedirectToAction("Profile", "Account");
        }

        try
        {
            var portalUrl = await _stripeService.CreateCustomerPortalSessionAsync(subscription.StripeCustomerId);
            return Redirect(portalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer portal for user {UserId}", user.Id);
            TempData["Error"] = "Error al acceder al portal de suscripcion.";
            return RedirectToAction("Profile", "Account");
        }
    }
}
