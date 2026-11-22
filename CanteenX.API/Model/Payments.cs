namespace CanteenX.API.Model
{
    public class Payments
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = null!;
        public string RazorPayOrderId { get; set; } = null!;
        public string RazorpayPaymentId { get; set; } = null!;
        public int Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual Order Order { get; set; } = null!;
    }

}
