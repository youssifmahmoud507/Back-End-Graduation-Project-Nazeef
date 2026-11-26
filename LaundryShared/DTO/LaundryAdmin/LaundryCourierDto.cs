namespace LaundryShared.DTO.LaundryAdmin
{
    public class LaundryCourierDto
    {
        public string CourierId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int CancellationCount { get; set; }
        public int? LaundryId { get; set; }
        public string? LaundryName { get; set; }
    }
}
