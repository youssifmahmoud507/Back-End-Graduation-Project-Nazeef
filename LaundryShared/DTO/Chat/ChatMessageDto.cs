using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.Chat
{
    /// <summary>
    /// رسالة واحدة في المحادثة
    /// بتتحفظ في الـ Chat History
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>user أو assistant</summary>
        public string Role { get; set; }

        /// <summary>محتوى الرسالة</summary>
        public string Content { get; set; }

        /// <summary>وقت الرسالة</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
