using System.ComponentModel.DataAnnotations;

namespace TlatoaniShared.Entities.Web;

public class Subscription
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public string? StripeCustomerId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    [Required]
    public string PlanName { get; set; } = "Premium";

    public decimal PriceAmountMxn { get; set; } = 299;

    [Required]
    public string Status { get; set; } = "active";

    public DateTime? CurrentPeriodStart { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CanceledAt { get; set; }
}
