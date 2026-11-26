using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LaundryShared.DTO.Chat
{
    /// <summary>
    /// الـ Request اللي العميل بيبعته للـ Chatbot
    /// </summary>
    public class ChatRequestDto
    {
        /// <summary>السؤال بتاع العميل</summary>
        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }

        /// <summary>
        /// تاريخ المحادثة
        /// بيتبعت من الـ Frontend عشان الـ AI يفتكر السياق
        /// </summary>
        public List<ChatMessageDto> History { get; set; } = new();
    }
}
