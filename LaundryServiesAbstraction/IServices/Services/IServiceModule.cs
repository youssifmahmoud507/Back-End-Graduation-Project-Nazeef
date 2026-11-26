using LaundryShared.Common;
using LaundryShared.DTO.Laundry;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Services
{
    public interface IServiceModule
    {
        // GET /api/services
        Task<Result<List<ServiceDto>>> GetAllServicesAsync();

        // GET /api/services/categories
        Task<Result<List<string>>> GetServiceCategoriesAsync();

        // GET /api/services/category/{category}
        Task<Result<List<ServiceDto>>> GetServicesByCategoryAsync(ServiceCategory category);
    }
}
