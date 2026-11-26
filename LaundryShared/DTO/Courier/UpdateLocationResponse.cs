namespace LaundryShared.DTO.Courier
{
    public class UpdateLocationResponse
    {
    // Response لـ Update Location
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double? DistanceToDestination { get; set; } // km
        public DateTime? EstimatedArrival { get; set; }
        public bool NearDestination { get; set; }
    }
}
