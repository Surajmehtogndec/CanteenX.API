namespace CanteenX.API.DTOs.Orders
{
    public class OrderItemResponseDto
    {
        public int OrderItemId { get;set; }
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public string UnitPrice { get; set; } = null!;
        public int Quantity { get; set; }
        public string SubTotal { get; set; } = null!;

    }
}
