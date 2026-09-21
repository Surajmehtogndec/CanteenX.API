using System.ComponentModel.DataAnnotations;

namespace CanteenX.API.DTOs.Admin
{
    public class CreateCanteenDto
    {


        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string? Location { get; set; }

        [Required]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public IFormFile? ImageUrl { get; set; }

        public bool IsActive { get; set; } 

    }
}
