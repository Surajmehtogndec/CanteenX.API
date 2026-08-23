
namespace CanteenX.API.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public UserInfoDto User { get; set; } = new();
    }

    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}
