using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LaundryShared.DTO.Matching
{

    /// <summary>
    /// الـ Request اللي العميل بيبعته
    /// عشان الـ AI يختارله أحسن مغسلة
    /// </summary>
    public class MatchingRequestDto
    {
        /// <summary>خط العرض بتاع موقع العميل</summary>
        [Required]
        public double Latitude { get; set; }

        /// <summary>خط الطول بتاع موقع العميل</summary>
        [Required]
        public double Longitude { get; set; }

        /// <summary>نوع الخدمة المطلوبة (غسيل / كوي / دراي كلين)</summary>
        public string? ServiceType { get; set; }

        /// <summary>أقصى ميزانية للعميل</summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>أقصى مسافة مقبولة بالكيلومتر</summary>
        public double? MaxDistanceKm { get; set; } = 10;
    }
}
