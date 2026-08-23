using CanteenX.API.DTOs;
namespace CanteenX.API.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> GoogleLoginAsync(string idToken);

        Task<AuthResponseDto?> GoogleRegisterAsync(string idToken);
    }
}
