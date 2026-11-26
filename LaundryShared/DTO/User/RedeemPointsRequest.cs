using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.User
{
    public class RedeemPointsRequest
    {
        [Required]
        public int Points { get; set; }

        [Required]
        public string RewardId { get; set; }
    }
}
