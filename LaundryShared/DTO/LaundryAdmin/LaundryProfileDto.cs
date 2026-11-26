using System;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class LaundryProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public LaundryStatus Status { get; set; }
        public LaundryAvailability Availability { get; set; }
        public string ImageUrl { get; set; }
        public float AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LaundryServiceDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
