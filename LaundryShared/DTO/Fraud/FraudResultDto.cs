using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.Fraud
{
    /// <summary>
    /// نتيجة تحليل الـ AI للعملية
    /// </summary>
    public class FraudResultDto
    {
        /// <summary>في مشكلة ولا لا</summary>
        public bool IsSuspicious { get; set; }

        /// <summary>درجة الخطورة من 0 لـ 100</summary>
        public int RiskScore { get; set; }

        /// <summary>سبب الاشتباه</summary>
        public string Reason { get; set; }

        /// <summary>الإجراء المقترح (Block / Warn / Allow)</summary>
        public string RecommendedAction { get; set; }
    }
}
