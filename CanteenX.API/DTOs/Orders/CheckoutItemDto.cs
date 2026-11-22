using System.ComponentModel.DataAnnotations;

namespace CanteenX.API.DTOs.Orders
{
    public class CheckoutItemDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int MenuItemId { get; set; }

        [Required]
        [Range(1, 50)]
        public int Quantity { get; set; }
    }

}
