namespace LaundryShared.DTO.Laundry
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
