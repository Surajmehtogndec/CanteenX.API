namespace CanteenX.API.Model
{
    public class Payments
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = null!;
        public string? RazorPayOrderId { get; set; } 
        public string? RazorpayPaymentId { get; set; } 
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentStatus { get; set; } = "Created";
        public string? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual Order Order { get; set; } = null!;
    }

}
