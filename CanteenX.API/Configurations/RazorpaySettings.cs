namespace CanteenX.API.Configurations
{
    public class RazorpaySettings
    {
        public string KeyId { get; set; } = null!;
        public string KeySecret { get; set; } = null!;
        public string Currency { get; set; } = "INR";
        public string CompanyName { get; set; } = "GNDEC CanteenX";
        public string Description { get; set; } = "GNDECCanteenX Food Order Payment";
    }
}
