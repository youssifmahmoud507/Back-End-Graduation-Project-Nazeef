using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleSearchParams
    {
        public string? Keyword { get; init; }
        public BundleStatus? Status { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public int? LaundryId { get; init; }
        public BundleOwnerType? OwnerType { get; init; }
        public int PageIndex { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}
