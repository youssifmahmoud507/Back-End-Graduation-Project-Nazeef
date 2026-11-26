namespace LaundryShared.DTO.Laundry
{
    public class LaundryParams
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? Search { get; set; }
        public string? Availability { get; set; }
        public float? MinRating { get; set; }
        public string? SortBy { get; set; } // rating, name
    }
}
