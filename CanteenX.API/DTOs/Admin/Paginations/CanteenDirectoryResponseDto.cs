namespace CanteenX.API.DTOs.Admin.Paginations
{
    public class CanteenDirectoryResponseDto
    {
        // Current page ke users
        public IReadOnlyList<CanteenDirectoryItemDto> Items { get; set; }
        = new List<CanteenDirectoryItemDto>();

        // Current page number
        public int PageNumber { get; set; }

        // Har page par records ki maximum sankhya
        public int PageSize { get; set; }

        // Search aur filters ke baad total matching users
        public int TotalRecords { get; set; }

        // Total available pages
        public int TotalPages { get; set; }

        // Kya previous page available hai?
        public bool HasPreviousPage => PageNumber > 1;

        // Kya next page available hai?
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
