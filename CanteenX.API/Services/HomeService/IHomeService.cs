using CanteenX.API.DTOs.CanteenOwner;

namespace CanteenX.API.Services.HomeService
{
    public interface IHomeService
    {
        Task<IEnumerable<CanteenCardDto>> GetAllActiveCanteensAsync( CancellationToken cancellationToken);
    }
}
