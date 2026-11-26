using LaundryShared.DTO.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryServiesAbstraction.IServices.AI
{
    public interface ISentimentService
    {
        Task<SentimentResultDto?> AnalyzeSentimentAsync(string comment, int rating);
    }
}
