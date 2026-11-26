using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.PaymentKashier
{
    /// <summary>
    /// الـ Request اللي العميل بيبعته عشان يشحن محفظته
    /// </summary>
    public class ChargeWalletRequest
    {
        /// <summary>المبلغ اللي عايز يشحنه (بالجنيه)</summary>
        [Required]
        [Range(10, 10000, ErrorMessage = "Amount must be between 10 and 10000 EGP")]
        public decimal Amount { get; set; }
    }
}
