using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.Laundry
{
    public class LaundryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Status { get; set; }
        public string Availability { get; set; }
        public float AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ServiceDto> Services { get; set; } = new List<ServiceDto>();
    }
}
