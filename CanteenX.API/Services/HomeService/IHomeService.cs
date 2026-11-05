using CanteenX.API.DTOs.CanteenOwner;

namespace CanteenX.API.Services.HomeService
{
    public interface IHomeService
    {
        Task<IEnumerable<MenuItemResponseDto>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<MenuItemResponseDto>> GetAvailableMenuItemsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CanteenCardDto>> GetAllActiveCanteensAsync(CancellationToken cancellationToken = default);
    }
}
