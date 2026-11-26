using System;

namespace LaundryShared.DTO.Fraud
{
    public class SuspiciousActivityDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ActionType { get; set; }
        public string Reason { get; set; }
        public int RiskScore { get; set; }
        public string IpAddress { get; set; }
        public string AiRecommendedAction { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}