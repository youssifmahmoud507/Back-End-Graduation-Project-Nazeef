using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.AI
{
    public class SentimentResultDto
    {
        public SentimentType Sentiment { get; set; }        // "Positive" / "Negative" / "Neutral"
        public float ConfidenceScore { get; set; }   // 0.0 - 1.0
        public string? Summary { get; set; }
    }
}
