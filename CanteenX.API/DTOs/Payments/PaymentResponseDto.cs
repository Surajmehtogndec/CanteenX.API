namespace CanteenX.API.DTOs.Payments
{
    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string RazorPayOrderId { get; set; } = null!;
        public int Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;

        public string RazorpayKeyId { get; set; } = null!;
    }
}
