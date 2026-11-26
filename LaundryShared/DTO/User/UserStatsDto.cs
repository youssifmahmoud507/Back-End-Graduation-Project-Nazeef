namespace LaundryShared.DTO.User
{
    public class UserStatsDto
    {
        public int CompletedOrders { get; set; }
        public int ActiveOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int Points { get; set; }
        public int SavedAddresses { get; set; }
        public int FavoriteLaundries { get; set; }
    }
}
