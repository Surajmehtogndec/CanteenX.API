namespace CanteenX.API.DTOs.Orders
{
    public class CanteenOrderResponseDto
    {
        public int CanteenOrderId { get; set; }
        public int CanteenId { get; set; }
        public string CanteenName { get; set; } = null!;
        public string SubTotal { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;

        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}
