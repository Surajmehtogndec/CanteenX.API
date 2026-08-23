namespace CanteenX.API.DTOs
{
    public class LoginDto
    {
        public string EmailOrPhone { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
