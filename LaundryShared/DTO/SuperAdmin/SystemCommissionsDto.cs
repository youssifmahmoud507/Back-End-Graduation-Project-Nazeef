using System;
using System.Collections.Generic;

namespace LaundryShared.DTO.SuperAdmin
{
    public class SystemCommissionsDto
    {
        public decimal TotalPlatformCommissions { get; set; }
        public decimal TotalPendingDebts { get; set; }
        public List<CommissionTransactionDto> RecentTransactions { get; set; } = new List<CommissionTransactionDto>();
        public List<LaundryDebtDto> LaundryDebts { get; set; } = new List<LaundryDebtDto>();
    }

    public class CommissionTransactionDto
    {
        public int Id { get; set; }
        public string LaundryName { get; set; }
        public int OrderId { get; set; }
        public decimal OrderAmount { get; set; }
        public decimal CommissionPercentage { get; set; }
        public decimal CommissionAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LaundryDebtDto
    {
        public int LaundryId { get; set; }
        public string LaundryName { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingCommission { get; set; }
        public decimal DebtCeiling { get; set; }
        public string Status { get; set; }
    }
}