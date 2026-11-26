using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Complaint
{
    public class ComplaintDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int LaundryId { get; set; }
        public string LaundryName { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string Details { get; set; }
        public ComplaintStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
