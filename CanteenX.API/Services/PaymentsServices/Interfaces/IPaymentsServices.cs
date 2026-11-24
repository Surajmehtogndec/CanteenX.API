using CanteenX.API.DTOs;
using CanteenX.API.DTOs.Payments;

namespace CanteenX.API.Services.PaymentsServices.Interfaces
{
    public interface IPaymentsServices
    {
      Task<PaymentResponseDto> CreatePaymentAsync(string userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default); 
      Task<PaymentResponseDto> VerifyPaymentAsync(string userId, PaymentVerificationDto request, CancellationToken cancellationToken = default);
      Task<PaymentResponseDto?> GetPaymentByOrderIdAsync(int orderId,string userId,CancellationToken cancellationToken = default);
    }
}
