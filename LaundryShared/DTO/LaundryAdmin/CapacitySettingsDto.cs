namespace LaundryShared.DTO.LaundryAdmin
{
    public class CapacitySettingsDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("maxOrdersPerDay")]
        public int MaxOrdersPerDay { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("leadTimeHours")]
        public int LeadTimeHours { get; set; }
    }
}
