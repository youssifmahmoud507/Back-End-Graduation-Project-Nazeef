using LaundryShared.Common;
using LaundryShared.DTO.Laundry;
using LaundryShared.DTO.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Laundry
{
    public interface ILaundryService
    {
        // GET /api/laundries
        Task<Result<Pagination<LaundryDto>>> GetAllLaundriesAsync(LaundryParams laundryParams);

        // GET /api/laundries/:id
        Task<Result<LaundryDto>> GetLaundryByIdAsync(int id);

        // GET /api/laundries/:id/services
        Task<Result<List<ServiceDto>>> GetLaundryServicesAsync(int laundryId);

        // GET /api/laundries/:id/reviews
        Task<Result<List<ReviewDto>>> GetLaundryReviewsAsync(int laundryId);

        // GET /api/laundries/search
        Task<Result<Pagination<LaundryDto>>> SearchLaundriesAsync(LaundrySearchParams searchParams);
    }
}
