using LaundryShared.Common;
using LaundryShared.DTO.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryServiesAbstraction.IServices.Chat
{
    /// <summary>
    /// عقد الخدمة بتاعة الـ Chatbot
    /// بيدعم الـ Streaming والـ Normal Response
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// بيبعت رسالة للـ AI ويرجع رد عادي
        /// بدون Streaming
        /// </summary>
        Task<Result<ChatResponseDto>> SendMessageAsync(
            string userId, ChatRequestDto request);

        /// <summary>
        /// بيبعت رسالة للـ AI ويرجع رد حرف حرف
        /// عن طريق Streaming (SSE)
        /// </summary>
        IAsyncEnumerable<string> SendMessageStreamAsync(
            string userId, ChatRequestDto request, CancellationToken cancellationToken = default);
    }
}
