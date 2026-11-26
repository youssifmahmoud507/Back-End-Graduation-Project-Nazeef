using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LaundryShared.DTO.Fraud
{
    /// <summary>
    /// الـ Request بتاع تحليل عملية دفع
    /// بيستخدمه الـ SuperAdmin بس
    /// </summary>
    public class AnalyzePaymentRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string IpAddress { get; set; }
    }
}
