using System;

namespace LaundryShared.DTO.SuperAdmin
{
    public class ProcessPayoutRequest
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? Reference { get; set; }
    }
}
