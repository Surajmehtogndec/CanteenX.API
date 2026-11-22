using System.ComponentModel.DataAnnotations;

namespace CanteenX.API.DTOs.Payments
{
    public class PaymentVerificationDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public string RazorPayOrderId { get; set; } = null!;

        [Required]
        public string RazorpayPaymentId { get; set; } = null!;

        [Required]
        public string RazorpaySignature { get; set; } = null!;


    }
}
