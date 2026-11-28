namespace CanteenX.API.Model
{
    public class CanteenOrder
    {
     public int CanteenOrderId { get; set; }
     public int OrderId { get; set; }
     public int CanteenId { get; set; }
     public string OrderStatus { get; set; } = "Pending";
     public string Subtotal { get; set; } = null!;
     public DateTime CreatedAt { get; set; }
     public DateTime? UpdatedAt { get; set; }
     public virtual Canteen Canteen { get; set; } = null!;
     public virtual Order Order { get; set; } = null!;
     public virtual ICollection<OrderItems> OrderItems { get; set; }
              = new List<OrderItems>();
    }
}
