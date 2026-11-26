using System;

namespace LaundryShared.DTO.Analytics
{
    public class OrderStatusDistributionDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TopLaundryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Orders { get; set; } = string.Empty; // formatted string e.g "1,240"
        public string Rev { get; set; } = string.Empty; // formatted string e.g "SAR 38.2K"
    }

    public class CityOrderRatioDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; } // Percentage integer
        public string Color { get; set; } = string.Empty;
    }

    public class RecentOrderDto
    {
        public string Id { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Laundry { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty; // e.g. "12 mins ago"
        public string Status { get; set; } = string.Empty;
    }
}
