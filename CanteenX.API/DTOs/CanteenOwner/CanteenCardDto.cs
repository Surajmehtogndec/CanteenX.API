using CanteenX.API.Model;

namespace CanteenX.API.DTOs.CanteenOwner
{
    public class CanteenCardDto
    {
        
        // 1. Basic Canteen Details
        public int CanteenId { get; set; }
        public string CanteenName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // 2. Flattened Metrics Details (Bina entity object bheje)
        public string CrowdStatus { get; set; } = string.Empty;
        public string EstimatedWaitTime { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        //public virtual Canteen? Canteen { get; set; }
    }


    
}
