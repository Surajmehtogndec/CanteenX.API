using CanteenX.API.DTOs.CanteenOwner;
namespace CanteenX.API.Services.CanteenOwnerService.Interface
{
    public interface ICanteenOwnerService
    {
        Task<bool> AddMenuItemAsync(AddMenuItemDto dto, CancellationToken cancellationToken);
    }
}
