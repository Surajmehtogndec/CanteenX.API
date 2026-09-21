using CanteenX.API.DTOs.Admin;
namespace CanteenX.API.Services.AdminServices
{
    public interface IAdminServices
    {
        Task<List<CanteenStaffOptionDto>> GetAvailableCanteenStaffAsync();
        Task<bool> AddCanteenAsync(CreateCanteenDto dto);
    }
}
