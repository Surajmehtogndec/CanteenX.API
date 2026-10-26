namespace CanteenX.API.DTOs.Admin.Paginations
{
    public sealed class CanteenDirectoryItemDto
    {
        public string CanteenName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
