using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.User
{
    public class AddAddressRequest
    {
        [Required]
        public AddressType Type { get; set; }

        [Required]
        [MaxLength(100)]
        public string Label { get; set; }

        [Required]
        [MaxLength(250)]
        public string Street { get; set; }

        [MaxLength(50)]
        public string? Apt { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [MaxLength(100)]
        public string Area { get; set; }

        [MaxLength(500)]
        public string? Instructions { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
