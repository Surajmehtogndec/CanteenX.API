using CanteenX.API.DTOs.Admin;
using CanteenX.API.DTOs.Admin.Paginations;
using Microsoft.AspNetCore.Identity;
namespace CanteenX.API.Services.AdminServices
{
    public interface IAdminServices
    {
        Task<List<CanteenStaffOptionDto>> GetAvailableCanteenStaffAsync();
        Task<bool> AddCanteenAsync(CreateCanteenDto dto);

        Task<UserProfileDtos?> GetProfileDataAsync(string userId);

        Task<IdentityResult?> UpdateProfileDataAsync(string userId, UserProfileDtos dto);

        Task<UserDirectoryResponseDto> GetUserDirectoryAsync(UserDirectoryRequestDto request,CancellationToken cancellationToken);
        Task<IdentityResult?> UpdateUserDirectoryAsync(string userId, UserDirectoryItemDto dto, CancellationToken cancellationToken);
    }
}
