using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.Chat
{

    /// <summary>
    /// الـ Response اللي بيرجع للعميل
    /// في حالة مش Streaming
    /// </summary>
    public class ChatResponseDto
    {
        /// <summary>رد الـ AI</summary>
        public string Message { get; set; }

        /// <summary>وقت الرد</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


