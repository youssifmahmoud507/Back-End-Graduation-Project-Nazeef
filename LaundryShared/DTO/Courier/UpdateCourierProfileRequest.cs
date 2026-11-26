namespace LaundryShared.DTO.Courier
{
    public class UpdateCourierProfileRequest
    // Update Courier Profile
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
