using CanteenX.API.DTOs.Payments;

namespace CanteenX.API.Services.PaymentsServices.Interfaces
{
    public interface IPaymentsServices
    {
        Task<(bool IsSuccess,string Message)> AddCanteenAccountAsync(CanteenAccountDetailsDto dto, CancellationToken cancellationToken);

    }
}
