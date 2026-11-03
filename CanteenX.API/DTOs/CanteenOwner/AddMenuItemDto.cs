namespace CanteenX.API.DTOs.CanteenOwner
{
    public class AddMenuItemDto
    {
        public int CanteenId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public IFormFile? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } 

    }

    
    
}
