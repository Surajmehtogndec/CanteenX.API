using CanteenX.API.DTOs.CanteenOwner;
namespace CanteenX.API.Services.CanteenOwnerService.Interface
{
    public interface ICanteenOwnerService
    {
        Task<int> GetCanteenIdByUserIdAsync(string userId);
        Task<bool> AddMenuItemAsync(AddMenuItemDto dto, CancellationToken cancellationToken);
        Task<CanteenProfileDto?> GetCanteenProfileDataAsync(string userId, CancellationToken cancellationToken);
    }
}
