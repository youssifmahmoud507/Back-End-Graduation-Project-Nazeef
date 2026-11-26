using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.LaundryAdmin
{
    /// <summary>
    /// البيانات الجديدة اللي صاحب المغسلة عايز يعدل بيها الخدمة
    /// </summary>
    public class UpdateServiceRequest
    {
        public string? ServiceName { get; set; }

        public ServiceCategory? Category { get; set; }

        [Range(0.01, 10000)]
        public decimal? Price { get; set; }

        public bool? IsAvailable { get; set; }
    }
}
