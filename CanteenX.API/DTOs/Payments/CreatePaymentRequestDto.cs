using System.ComponentModel.DataAnnotations;

namespace CanteenX.API.DTOs.Payments
{
    public class CreatePaymentRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int OrderId { get; set; }
    }
}
