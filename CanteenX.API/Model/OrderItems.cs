namespace CanteenX.API.Model
{
    public class OrderItems
    {
        public int OrderItemId { get; set; }
        public int CanteenOrderId { get; set; }
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public string UnitPrice { get; set; } = null!;

        public int Quantity { get; set; }
        public string SubTotal { get; set; } = null!;
        public virtual CanteenOrder CanteenOrder { get; set; } = null!;
        public virtual MenuItems MenuItem { get; set; } = null!;
    }
}
