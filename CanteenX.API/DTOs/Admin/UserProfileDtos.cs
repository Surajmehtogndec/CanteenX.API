
namespace CanteenX.API.DTOs.Admin
{
    public class UserProfileDtos
    {
        
        public string FullName { get; set; } = string.Empty;

       
        public string Email { get; set; } = string.Empty;

       
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
