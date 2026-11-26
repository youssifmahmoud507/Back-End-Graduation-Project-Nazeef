using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class CreateBundleItemRequest
    {
        public required int ServiceId { get; init; }
        public required int Quantity { get; init; }
        public ServiceCategory? AllowedItemCategory { get; init; }
    }
}
