using LaundryShared.Common;
using LaundryShared.DTO.Matching;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryServiesAbstraction.IServices.Matching
{
    /// <summary>
    /// عقد الخدمة بتاعة الـ Smart Matching
    /// بيختار أحسن مغسلة للعميل بناءً على موقعه وتفضيلاته
    /// </summary>
    public interface IMatchingService
    {
        /// <summary>
        /// بيجيب أحسن مغسلة للعميل
        /// بيستخدم Gemini عشان يحلل ويختار
        /// </summary>
        Task<Result<MatchingResponseDto>> GetBestMatchAsync(
            string customerId, MatchingRequestDto request);

        /// <summary>
        /// بيجيب أحسن 3 مغاسل للعميل
        /// عشان يختار منهم
        /// </summary>
        Task<Result<List<MatchingResponseDto>>> GetTopMatchesAsync(
            string customerId, MatchingRequestDto request);
    }
}
