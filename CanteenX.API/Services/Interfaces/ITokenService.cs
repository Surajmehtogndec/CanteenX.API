using CanteenX.API.Model;

namespace CanteenX.API.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles);
        RefreshToken GenerateRefreshToken(string userId, string? ipAddress);

    }
}
