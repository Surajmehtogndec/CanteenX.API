namespace CanteenX.API.DTOs.Orders
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public string TotalAmount { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = null!;

    }
}
