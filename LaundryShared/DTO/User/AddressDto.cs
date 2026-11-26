using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.User
{
    public class AddressDto
    {
        public int Id { get; set; }
        public AddressType Type { get; set; }
        public string Label { get; set; }
        public string Street { get; set; }
        public string? Apt { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string? Instructions { get; set; }
        public bool IsDefault { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
