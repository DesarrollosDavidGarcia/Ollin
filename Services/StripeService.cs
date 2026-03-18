using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TlatoaniShared.Data;
using TlatoaniShared.Entities.Web;

namespace Ollin.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string userId, string email);
    Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId);
    Task HandleWebhookAsync(string json, string signature);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<StripeService> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(string userId, string email)
    {
        var options = new SessionCreateOptions
        {
            CustomerEmail = email,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = _config["Stripe:PriceId"],
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = _config["Stripe:SuccessUrl"],
            CancelUrl = _config["Stripe:CancelUrl"],
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId)
    {
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = _config["Ollin:SiteUrl"] + "/Account/Profile"
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string signature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompleted(stripeEvent);
                break;

            case EventTypes.InvoicePaymentSucceeded:
                await HandleInvoicePaymentSucceeded(stripeEvent);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var userId = session.Metadata.GetValueOrDefault("userId");
        if (string.IsNullOrEmpty(userId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TlatoaniDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return;

        // Update user tier
        user.SubscriptionTier = "Premium";
        await userManager.UpdateAsync(user);

        // Add claim and role
        var existingClaims = await userManager.GetClaimsAsync(user);
        var tierClaim = existingClaims.FirstOrDefault(c => c.Type == "SubscriptionTier");
        if (tierClaim != null)
            await userManager.RemoveClaimAsync(user, tierClaim);
        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("SubscriptionTier", "Premium"));

        if (!await userManager.IsInRoleAsync(user, "Premium"))
            await userManager.AddToRoleAsync(user, "Premium");

        // Create subscription record
        var subscription = new TlatoaniShared.Entities.Web.Subscription
        {
            UserId = userId,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            PlanName = "Premium",
            PriceAmountMxn = 299,
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        _logger.LogInformation("Subscription created for user {UserId}", userId);
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TlatoaniDbContext>();

        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeCustomerId == invoice.CustomerId && s.Status == "active");

        if (subscription != null)
        {
            subscription.CurrentPeriodStart = DateTime.UtcNow;
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            await db.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TlatoaniDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription == null) return;

        subscription.Status = "canceled";
        subscription.CanceledAt = DateTime.UtcNow;

        var user = await userManager.FindByIdAsync(subscription.UserId);
        if (user != null)
        {
            user.SubscriptionTier = "Free";
            await userManager.UpdateAsync(user);

            var claims = await userManager.GetClaimsAsync(user);
            var tierClaim = claims.FirstOrDefault(c => c.Type == "SubscriptionTier");
            if (tierClaim != null)
                await userManager.RemoveClaimAsync(user, tierClaim);

            if (await userManager.IsInRoleAsync(user, "Premium"))
                await userManager.RemoveFromRoleAsync(user, "Premium");
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Subscription canceled for user {UserId}", subscription.UserId);
    }
}
