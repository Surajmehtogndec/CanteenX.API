using Microsoft.AspNetCore.Identity;

namespace CanteenX.API.Model
{
    public class ApplicationUser : IdentityUser
    {


        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Canteen? Canteen { get; set; }

    }
}
