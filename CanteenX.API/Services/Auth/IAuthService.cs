using CanteenX.API.DTOs;
namespace CanteenX.API.Services.Auth
{
    public interface IAuthService
    {
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ValidateResetTokenAsync( string email,string token);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

        Task<AuthResponseDto> RegisterAdminAsync(RegisterDto dto);

        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> GoogleLoginAsync(string idToken);

        Task<AuthResponseDto?> GoogleRegisterAsync(string idToken);
    }
}
