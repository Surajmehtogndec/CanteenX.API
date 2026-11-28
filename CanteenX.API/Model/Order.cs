using Razorpay.Api;

namespace CanteenX.API.Model
{
    public partial class Order
    {
     public int OrderId { get; set; }
     public string UserId { get; set; } = null!;
     public string TotalAmount { get; set; } = null!;
     public string OrderStatus { get; set; } = "Pending";
     public string PaymentStatus { get; set; } = "Pending";
     public DateTime CreatedAt { get; set; }
     public DateTime? UpdatedAt { get; set; }

     public virtual ApplicationUser User { get; set; } = null!;
     public virtual ICollection<CanteenOrder> CanteenOrders { get; set; }
            = new List<CanteenOrder>();
     public virtual ICollection<Payments> Payments { get; set; }
               = new List<Payments>();
    }
    }
