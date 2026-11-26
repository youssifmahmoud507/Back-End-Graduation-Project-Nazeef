using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Specifications.ServiceSpecs;
using LaundryServiesAbstraction.IServices.Services;
using LaundryShared.Common;
using LaundryShared.DTO.Laundry;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.ServicesModule
{
    public class ServiceModule : IServiceModule
    {
        private readonly IUnitOfWork _unitOfWork;

        public ServiceModule(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<ServiceDto>>> GetAllServicesAsync()
        {
            var spec = new AllAvailableServicesSpec();

            var services = await _unitOfWork.Repository<Service, int>()
                                            .GetAllWithSpecAsync(spec);

            if (!services.Any())
                return Result<List<ServiceDto>>.Failure("No services found.");

            var data = services.Select(s => new ServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Category = s.Category.ToString(),
                Price = s.Price,
                IsAvailable = s.IsAvailable
            }).ToList();

            return Result<List<ServiceDto>>.Success(data);
        }

        public async Task<Result<List<string>>> GetServiceCategoriesAsync()
        {
            var categories = Enum.GetNames(typeof(ServiceCategory)).ToList();
            return Result<List<string>>.Success(categories);
        }

        public async Task<Result<List<ServiceDto>>> GetServicesByCategoryAsync(ServiceCategory category)
        {
            var spec = new ServicesByCategorySpec(category);

            var services = await _unitOfWork.Repository<Service, int>()
                                            .GetAllWithSpecAsync(spec);

            if (!services.Any())
                return Result<List<ServiceDto>>.Failure("No services found for this category.");

            var data = services.Select(s => new ServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Category = s.Category.ToString(),
                Price = s.Price,
                IsAvailable = s.IsAvailable
            }).ToList();

            return Result<List<ServiceDto>>.Success(data);
        }
    }
}
