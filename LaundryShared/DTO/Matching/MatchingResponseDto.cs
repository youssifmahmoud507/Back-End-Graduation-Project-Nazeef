using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.Matching
{
    
 
        /// <summary>
        /// الـ Response بتاع الـ AI
        /// بيرجع أحسن مغسلة مع السبب
        /// </summary>
        public class MatchingResponseDto
        {
            /// <summary>الـ ID بتاع المغسلة المختارة</summary>
            public int LaundryId { get; set; }

            /// <summary>اسم المغسلة</summary>
            public string LaundryName { get; set; }

            /// <summary>عنوان المغسلة</summary>
            public string Address { get; set; }

            /// <summary>المسافة بالكيلومتر</summary>
            public double DistanceKm { get; set; }

            /// <summary>متوسط التقييم</summary>
            public double Rating { get; set; }

            /// <summary>سبب اختيار الـ AI للمغسلة دي</summary>
            public string Reason { get; set; }

            /// <summary>الوقت المتوقع للتسليم</summary>
            public string EstimatedDeliveryTime { get; set; }

            /// <summary>قايمة بأحسن 3 مغاسل</summary>
            public List<MatchingResponseDto> TopMatches { get; set; } = new();
        }
    }
