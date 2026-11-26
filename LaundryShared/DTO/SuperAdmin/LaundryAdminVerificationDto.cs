using LaundryShared.Enums;
using System;

namespace LaundryShared.DTO.SuperAdmin
{
    public class LaundryAdminVerificationDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsIdentityVerified { get; set; }
        public string? IdCardImageUrl { get; set; }
        public string? CommercialRegisterDocumentUrl { get; set; }
        public LaundryAdminApprovalStatus? AdminApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewVerificationRequestDto
    {
        public string? RejectionReason { get; set; }
    }
}