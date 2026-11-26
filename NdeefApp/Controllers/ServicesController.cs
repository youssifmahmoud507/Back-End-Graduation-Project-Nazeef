using LaundryServiesAbstraction.IServices.Services;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceModule _serviceModule;

        public ServicesController(IServiceModule serviceModule)
        {
            _serviceModule = serviceModule;
        }

        // GET /api/services
        [HttpGet]
        [Authorize(Roles = "Customer,LaundryAdmin,SuperAdmin")]
        public async Task<IActionResult> GetAllServices()
        {
            var result = await _serviceModule.GetAllServicesAsync();

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/services/categories
        [HttpGet("categories")]
        [Authorize(Roles = "Customer,LaundryAdmin,SuperAdmin")]
        public async Task<IActionResult> GetServiceCategories()
        {
            var result = await _serviceModule.GetServiceCategoriesAsync();
            return Ok(result.Value);
        }

        // GET /api/services/category/{category}
        [HttpGet("category/{category}")]
        [Authorize(Roles = "Customer,LaundryAdmin,SuperAdmin")]
        public async Task<IActionResult> GetServicesByCategory(ServiceCategory category)
        {
            var result = await _serviceModule.GetServicesByCategoryAsync(category);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }
    }
}
