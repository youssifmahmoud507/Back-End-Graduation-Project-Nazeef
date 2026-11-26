namespace LaundryShared.DTO.Laundry
{
    public class LaundrySearchParams
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // Search by Name
        public string? Q { get; set; }

        // Search by Location
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public double? Radius { get; set; } // بالكيلومتر
    }
}
