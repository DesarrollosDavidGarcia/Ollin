using Microsoft.AspNetCore.Identity;

namespace TlatoaniShared.Entities.Web;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public string SubscriptionTier { get; set; } = "Free";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
