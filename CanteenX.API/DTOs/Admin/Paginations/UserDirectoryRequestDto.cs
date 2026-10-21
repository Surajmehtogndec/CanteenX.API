namespace CanteenX.API.DTOs.Admin.Paginations
{
    public sealed class UserDirectoryRequestDto
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1
                ? 10
                : Math.Min(value, MaxPageSize);
        }                                      
        //public string? SearchTerm { get; set; }
        //public string? Status { get; set; }

        //public string? Role { get; set; }

        //public string SortBy { get; set; } = "FullName";

        //public string SortDirection { get; set; } = "asc";

    }
}
