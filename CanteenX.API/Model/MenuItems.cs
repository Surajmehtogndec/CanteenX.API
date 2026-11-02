namespace CanteenX.API.Model
{
    public partial class MenuItems
    {
        public int MenuItemId { get; set; }
        public int CanteenId { get; set; }
        public string ItemName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual Canteen Canteen { get; set; } = null!;
    }
}
