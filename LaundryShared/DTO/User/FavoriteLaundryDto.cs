namespace LaundryShared.DTO.User
{
    public class FavoriteLaundryDto
    {
        public int Id { get; set; }
        public int LaundryId { get; set; }
        public string LaundryName { get; set; }
        public string LaundryAddress { get; set; }
        public float AverageRating { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
