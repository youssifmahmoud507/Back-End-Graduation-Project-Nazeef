using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Courier
{
    public class ReportIssueRequest
    {
        [Required]
        [MaxLength(500)]
        public string IssueDescription { get; set; } = string.Empty;
    }
}