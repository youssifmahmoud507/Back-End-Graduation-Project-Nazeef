using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Specifications.LaundrySpecs;
using LaundryServiesAbstraction.IServices.Laundry;
using LaundryShared.Common;
using LaundryShared.DTO.Laundry;
using LaundryShared.DTO.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.LaundryServicesModule
{
    public class LaundryService : ILaundryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LaundryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<Result<Pagination<LaundryDto>>> GetAllLaundriesAsync(LaundryParams laundryParams)
        {
            var spec = new LaundryWithServicesSpec(laundryParams);
            var countSpec = new LaundryForCountSpec(laundryParams);

            var laundries = await _unitOfWork.Repository<Laundry, int>()
                                             .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Laundry, int>()
                                         .CountAsync(countSpec);

            var data = laundries.Select(l => new LaundryDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Status = l.Status.ToString(),
                Availability = l.Availability.ToString(),
                AverageRating = l.AverageRating,
                TotalReviews = l.TotalReviews,
                Services = l.Services.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Category = s.Category.ToString(),
                    Price = s.Price,
                    IsAvailable = s.IsAvailable
                }).ToList()
            }).ToList();

            var pagination = new Pagination<LaundryDto>(
                laundryParams.PageIndex,
                laundryParams.PageSize,
                count,
                data
            );

            return Result<Pagination<LaundryDto>>.Success(pagination);
        }

        public async Task<Result<LaundryDto>> GetLaundryByIdAsync(int id)
        {
            var spec = new LaundryWithServicesSpec(id);

            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (laundry == null)
                return Result<LaundryDto>.Failure("Laundry not found.");

            var dto = new LaundryDto
            {
                Id = laundry.Id,
                Name = laundry.Name,
                Address = laundry.Address,
                Latitude = laundry.Latitude,
                Longitude = laundry.Longitude,
                Status = laundry.Status.ToString(),
                Availability = laundry.Availability.ToString(),
                AverageRating = laundry.AverageRating,
                TotalReviews = laundry.TotalReviews,
                Services = laundry.Services.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Category = s.Category.ToString(),
                    Price = s.Price,
                    IsAvailable = s.IsAvailable
                }).ToList()
            };

            return Result<LaundryDto>.Success(dto);
        }

        public async Task<Result<List<ServiceDto>>> GetLaundryServicesAsync(int laundryId)
        {
            var spec = new LaundryWithServicesSpec(laundryId);

            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (laundry == null)
                return Result<List<ServiceDto>>.Failure("Laundry not found.");

            var services = laundry.Services.Select(s => new ServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Category = s.Category.ToString(),
                Price = s.Price,
                IsAvailable = s.IsAvailable
            }).ToList();

            return Result<List<ServiceDto>>.Success(services);
        }

        public async Task<Result<List<ReviewDto>>> GetLaundryReviewsAsync(int laundryId)
        {
            var spec = new LaundryWithReviewsSpec(laundryId);

            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (laundry == null)
                return Result<List<ReviewDto>>.Failure("Laundry not found.");

            var reviews = laundry.Reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CustomerName = r.Customer.Name,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Result<List<ReviewDto>>.Success(reviews);
        }

        public async Task<Result<Pagination<LaundryDto>>> SearchLaundriesAsync(LaundrySearchParams searchParams)
        {
            var spec = new LaundrySearchSpec(searchParams);
            var countSpec = new LaundrySearchCountSpec(searchParams);

            var laundries = await _unitOfWork.Repository<Laundry, int>()
                                             .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Laundry, int>()
                                         .CountAsync(countSpec);

            var data = laundries.Select(l => new LaundryDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Status = l.Status.ToString(),
                Availability = l.Availability.ToString(),
                AverageRating = l.AverageRating,
                TotalReviews = l.TotalReviews
            }).ToList();

            var pagination = new Pagination<LaundryDto>(
                searchParams.PageIndex,
                searchParams.PageSize,
                count,
                data
            );

            return Result<Pagination<LaundryDto>>.Success(pagination);
        }
    }
}

