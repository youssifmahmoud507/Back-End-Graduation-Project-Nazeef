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
    /// بيانات الخدمة الجديدة اللي صاحب المغسلة عايز يضيفها
    /// </summary>
    public class AddServiceRequest
    {
        [Required(ErrorMessage = "Service name is required")]
        public string ServiceName { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public ServiceCategory Category { get; set; }

        [Required, Range(0.01, 10000)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
