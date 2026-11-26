namespace LaundryShared.DTO.Courier
{
    public class CourierProfileDto
    {
    // Courier Profile DTO
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailable { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int CancellationCount { get; set; }
        public int? LaundryId { get; set; }
        public string? LaundryName { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
    }
}
