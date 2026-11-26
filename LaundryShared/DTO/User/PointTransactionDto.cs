using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.User
{
    public class PointTransactionDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public PointTransactionType Type { get; set; }
        public string Description { get; set; }
        public int? OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
