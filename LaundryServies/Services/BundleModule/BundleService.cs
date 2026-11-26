using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LaundryDomain.IRepositories;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.BundleSpecs;
using LaundryServiesAbstraction.IServices.BundleModule;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryShared.Common;
using LaundryShared.DTO.BundleModule;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.Extensions.Logging;

namespace LaundryServies.Services.BundleModule
{
    public class BundleService : IBundleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BundleService> _logger;

        public BundleService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<BundleService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ================== SUPERADMIN METHODS ==================
        
        public async Task<Result<BundleDto>> CreateBundleAsync(CreateBundleRequest request, string adminId)
        {
            if (request.OriginalPrice <= request.BundlePrice)
                return Result<BundleDto>.Failure("Bundle price must be less than original price");
                
            if (request.BundleItems == null || !request.BundleItems.Any())
                return Result<BundleDto>.Failure("Bundle must contain at least one item");

            if (request.LaundryId.HasValue)
            {
                var laundryRepo = _unitOfWork.Repository<Laundry, int>();
                var laundry = await laundryRepo.GetByIdAsync(request.LaundryId.Value);
                if (laundry == null)
                    return Result<BundleDto>.Failure($"Laundry not found: {request.LaundryId.Value}");
            }

            var serviceRepo = _unitOfWork.Repository<LaundryDomain.Models.LaundryModules.Service, int>();
            foreach (var item in request.BundleItems)
            {
                var service = await serviceRepo.GetByIdAsync(item.ServiceId);
                if (service == null)
                    return Result<BundleDto>.Failure($"Service not found: {item.ServiceId}");
            }

            var bundle = new Bundle
            {
                Name = request.Name,
                Description = request.Description,
                BundlePrice = request.BundlePrice,
                OriginalPrice = request.OriginalPrice,
                StartDate = request.StartDate,
                ExpiryDate = request.ExpiryDate,
                ImageUrl = request.ImageUrl,
                DisplayOrder = request.DisplayOrder,
                OwnerType = BundleOwnerType.Platform,
                LaundryId = request.LaundryId,
                CreatedByAdminId = adminId,
                Status = request.SetActiveImmediately ? BundleStatus.Active : BundleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                BundleItems = request.BundleItems.Select(x => new BundleItem 
                {
                    ServiceId = x.ServiceId,
                    Quantity = x.Quantity,
                    AllowedItemCategory = x.AllowedItemCategory
                }).ToList()
            };

            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            await bundleRepo.AddAsync(bundle);
            await _unitOfWork.CompleteAsync();

            return Result<BundleDto>.Success(MapToDto(bundle));
        }

        public async Task<Result<BundleDto>> UpdateBundleAsync(Guid bundleId, UpdateBundleRequest request, string adminId)
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = await bundleRepo.GetEntityWithSpecAsync(new BundleByIdWithItemsSpec(bundleId));
            
            if (bundle == null) return Result<BundleDto>.Failure("Bundle not found");
            if (bundle.Status == BundleStatus.Archived) return Result<BundleDto>.Failure("Archived bundles cannot be edited");

            if (request.Name != null) bundle.Name = request.Name;
            if (request.Description != null) bundle.Description = request.Description;
            if (request.BundlePrice.HasValue) bundle.BundlePrice = request.BundlePrice.Value;
            if (request.OriginalPrice.HasValue) bundle.OriginalPrice = request.OriginalPrice.Value;
            if (request.StartDate.HasValue) bundle.StartDate = request.StartDate;
            if (request.ExpiryDate != null) bundle.ExpiryDate = request.ExpiryDate;

            if (request.BundleItems != null && request.BundleItems.Any())
            {
                 var serviceRepo = _unitOfWork.Repository<LaundryDomain.Models.LaundryModules.Service, int>();
                 foreach (var item in request.BundleItems)
                 {
                     var svc = await serviceRepo.GetByIdAsync(item.ServiceId);
                     if (svc == null) return Result<BundleDto>.Failure($"Service not found: {item.ServiceId}");
                 }
                 bundle.BundleItems.Clear();
                 foreach(var itm in request.BundleItems)
                 {
                     bundle.BundleItems.Add(new BundleItem { ServiceId = itm.ServiceId, Quantity = itm.Quantity, AllowedItemCategory = itm.AllowedItemCategory });
                 }
            }

            bundle.UpdatedAt = DateTime.UtcNow;
            var wasActive = bundle.Status == BundleStatus.Active;
            
            bundleRepo.Update(bundle);
            await _unitOfWork.CompleteAsync();
            
            if (wasActive && bundle.LaundryId.HasValue)
            {
                // In a real app we might get the adminId of the laundry to notify.
                // Assuming we just use some notification.
            }

            return Result<BundleDto>.Success(MapToDto(bundle));
        }

        public async Task<Result> SetBundleStatusAsync(Guid bundleId, BundleStatus newStatus, string adminId)
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = await bundleRepo.GetByIdAsync(bundleId);
            if (bundle == null) return Result.Failure("Bundle not found");

            bool valid = false;
            switch(bundle.Status)
            {
                case BundleStatus.Draft: valid = (newStatus == BundleStatus.Active || newStatus == BundleStatus.Archived); break;
                case BundleStatus.PendingApproval: valid = false; break;
                case BundleStatus.Active: valid = (newStatus == BundleStatus.Inactive || newStatus == BundleStatus.Archived); break; // Spec missing Expired from Active transition list but prompt had matrix Draft->.., Active->Inactive,Archived. Actually Prompt: Active -> Inactive, Archived.
                case BundleStatus.Inactive: valid = (newStatus == BundleStatus.Active || newStatus == BundleStatus.Archived); break;
                case BundleStatus.Rejected: valid = (newStatus == BundleStatus.Archived); break;
                case BundleStatus.Expired: valid = (newStatus == BundleStatus.Archived); break;
                case BundleStatus.Archived: valid = false; break;
            }

            if (!valid) return Result.Failure($"Invalid status transition from {bundle.Status} to {newStatus}");

            bundle.Status = newStatus;
            bundle.UpdatedAt = DateTime.UtcNow;

            bundleRepo.Update(bundle);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Pagination<BundleApprovalRequestDto>> GetPendingApprovalsAsync(int pageIndex, int pageSize)
        {
            var repo = _unitOfWork.Repository<BundleApprovalRequest, Guid>();
            var spec = new BundleApprovalRequestsByStatusSpec(BundleApprovalStatus.Pending, pageIndex, pageSize);
            var results = await repo.GetAllWithSpecAsync(spec);
            
            var countSpec = new BundleApprovalRequestsByStatusCountSpec(BundleApprovalStatus.Pending);
            var count = await repo.CountAsync(countSpec);
            
            var dtos = results.Select(MapToApprovalDto).ToList();
            
            return new Pagination<BundleApprovalRequestDto>(pageIndex, pageSize, count, dtos);
        }

        public async Task<Result> ReviewBundleApprovalAsync(ReviewBundleApprovalRequest request, string superAdminId)
        {
            if (request.Decision != BundleApprovalStatus.Approved && request.Decision != BundleApprovalStatus.Rejected)
                return Result.Failure("Invalid decision");

            var repo = _unitOfWork.Repository<BundleApprovalRequest, Guid>();
            var approvalReq = await repo.GetEntityWithSpecAsync(new BundleApprovalRequestByIdSpec(request.ApprovalRequestId));
            if (approvalReq == null) return Result.Failure("Approval request not found");
            if (approvalReq.Status == BundleApprovalStatus.Approved || approvalReq.Status == BundleApprovalStatus.Rejected) 
                return Result.Failure("This request has already been reviewed");

            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = approvalReq.Bundle;

            if (request.Decision == BundleApprovalStatus.Approved)
            {
                if (!approvalReq.IsEditRequest)
                {
                    bundle.Status = BundleStatus.Active;
                }
                else
                {
                    if (!string.IsNullOrEmpty(approvalReq.PendingBundleSnapshot))
                    {
                        var snapshot = JsonSerializer.Deserialize<UpdateBundleRequest>(approvalReq.PendingBundleSnapshot);
                        if (snapshot != null)
                        {
                            if (snapshot.Name != null) bundle.Name = snapshot.Name;
                            if (snapshot.Description != null) bundle.Description = snapshot.Description;
                            if (snapshot.BundlePrice.HasValue) bundle.BundlePrice = snapshot.BundlePrice.Value;
                            if (snapshot.OriginalPrice.HasValue) bundle.OriginalPrice = snapshot.OriginalPrice.Value;
                            if (snapshot.BundleItems != null)
                            {
                                bundle.BundleItems.Clear();
                                foreach(var item in snapshot.BundleItems)
                                {
                                    bundle.BundleItems.Add(new BundleItem { ServiceId = item.ServiceId, Quantity = item.Quantity, AllowedItemCategory = item.AllowedItemCategory });
                                }
                            }
                        }
                    }
                }
                
                bundle.UpdatedAt = DateTime.UtcNow;
                approvalReq.Status = BundleApprovalStatus.Approved;
            }
            else // Rejected
            {
                if (!approvalReq.IsEditRequest)
                {
                    bundle.Status = BundleStatus.Rejected;
                }
                approvalReq.Status = BundleApprovalStatus.Rejected;
            }

            approvalReq.ReviewedByAdminId = superAdminId;
            approvalReq.ReviewedAt = DateTime.UtcNow;
            approvalReq.ReviewNote = request.ReviewNote;

            repo.Update(approvalReq);
            bundleRepo.Update(bundle);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        // ================== LAUNDRYADMIN METHODS ==================

        public async Task<Result<BundleApprovalRequestDto>> ProposeBundleAsync(ProposeBundleRequest request, string laundryAdminId)
        {
            var laundryRepo = _unitOfWork.Repository<Laundry, int>();
            var laundry = await laundryRepo.GetEntityWithSpecAsync(new LaundryByAdminIdSpec(laundryAdminId));
            if (laundry == null) return Result<BundleApprovalRequestDto>.Failure("Laundry not found for this admin");

            if (request.OriginalPrice <= request.BundlePrice) 
                return Result<BundleApprovalRequestDto>.Failure("Bundle price must be less than original price");
            if (request.BundleItems == null || !request.BundleItems.Any()) 
                return Result<BundleApprovalRequestDto>.Failure("Bundle must contain at least one item");

             var serviceRepo = _unitOfWork.Repository<LaundryDomain.Models.LaundryModules.Service, int>();
             var servicesSpec = new ServicesByLaundryIdSpec(laundry.Id);
             var availableServices = await serviceRepo.GetAllWithSpecAsync(servicesSpec);
             
             foreach (var item in request.BundleItems)
             {
                 if (!availableServices.Any(s => s.Id == item.ServiceId))
                     return Result<BundleApprovalRequestDto>.Failure($"Service {item.ServiceId} does not belong to your laundry");
             }

            var bundle = new Bundle
            {
                Name = request.Name,
                Description = request.Description,
                BundlePrice = request.BundlePrice,
                OriginalPrice = request.OriginalPrice,
                StartDate = request.StartDate,
                ExpiryDate = request.ExpiryDate,
                ImageUrl = request.ImageUrl,
                DisplayOrder = request.DisplayOrder,
                OwnerType = BundleOwnerType.Laundry,
                LaundryId = laundry.Id,
                CreatedByAdminId = laundryAdminId,
                Status = BundleStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                BundleItems = request.BundleItems.Select(x => new BundleItem 
                {
                    ServiceId = x.ServiceId,
                    Quantity = x.Quantity,
                    AllowedItemCategory = x.AllowedItemCategory
                }).ToList()
            };

            var appReq = new BundleApprovalRequest
            {
                Status = BundleApprovalStatus.Pending,
                IsEditRequest = false,
                RequestNote = request.RequestNote,
                RequestedAt = DateTime.UtcNow,
                RequestedByAdminId = laundryAdminId,
                Bundle = bundle
            };

            var reqRepo = _unitOfWork.Repository<BundleApprovalRequest, Guid>();
            await reqRepo.AddAsync(appReq);
            await _unitOfWork.CompleteAsync();
            
            return Result<BundleApprovalRequestDto>.Success(MapToApprovalDto(appReq));
        }

        public async Task<Result<BundleApprovalRequestDto>> ProposeEditBundleAsync(ProposeEditBundleRequest request, string laundryAdminId)
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = await bundleRepo.GetEntityWithSpecAsync(new BundleByIdWithItemsSpec(request.BundleId));
            
            if (bundle == null) return Result<BundleApprovalRequestDto>.Failure("Bundle not found");
            if (bundle.CreatedByAdminId != laundryAdminId) return Result<BundleApprovalRequestDto>.Failure("You do not have permission to edit this bundle");
            if (bundle.OwnerType == BundleOwnerType.Platform) return Result<BundleApprovalRequestDto>.Failure("Platform bundles cannot be edited by laundry admins");
            if (bundle.Status != BundleStatus.Active && bundle.Status != BundleStatus.Inactive) return Result<BundleApprovalRequestDto>.Failure("Only active or inactive bundles can have edit requests");

            var reqRepo = _unitOfWork.Repository<BundleApprovalRequest, Guid>();
            var existingReqs = await reqRepo.GetAllAsync(); // Ideal would be a spec: BundleApprovalRequestsByBundleIdAndStatusSpec
            if (existingReqs.Any(r => r.BundleId == request.BundleId && r.Status == BundleApprovalStatus.Pending))
                return Result<BundleApprovalRequestDto>.Failure("An edit request is already pending for this bundle");

            var snapshot = new UpdateBundleRequest
            {
                BundleId = request.BundleId,
                Name = request.Name,
                Description = request.Description,
                BundlePrice = request.BundlePrice,
                OriginalPrice = request.OriginalPrice,
                StartDate = request.StartDate,
                ExpiryDate = request.ExpiryDate,
                BundleItems = request.BundleItems
            };

            var appReq = new BundleApprovalRequest
            {
                BundleId = request.BundleId,
                RequestedByAdminId = laundryAdminId,
                Status = BundleApprovalStatus.Pending,
                IsEditRequest = true,
                RequestedAt = DateTime.UtcNow,
                RequestNote = request.RequestNote,
                PendingBundleSnapshot = JsonSerializer.Serialize(snapshot)
            };

            await reqRepo.AddAsync(appReq);
            await _unitOfWork.CompleteAsync();

            return Result<BundleApprovalRequestDto>.Success(MapToApprovalDto(appReq));
        }

        public async Task<Result> DeactivateBundleAsync(Guid bundleId, string adminId)
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = await bundleRepo.GetByIdAsync(bundleId);
            if (bundle == null) return Result.Failure("Bundle not found");

            var laundryRepo = _unitOfWork.Repository<Laundry, int>();
            var laundry = await laundryRepo.GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));
            
            if (laundry != null)
            {
                // Caller is LaundryAdmin
                if (bundle.LaundryId != laundry.Id) return Result.Failure("You can only deactivate your own laundry bundles");
            }
            
            if (bundle.Status != BundleStatus.Active) return Result.Failure("Only active bundles can be deactivated");

            bundle.Status = BundleStatus.Inactive;
            bundle.UpdatedAt = DateTime.UtcNow;
            
            bundleRepo.Update(bundle);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<IReadOnlyList<BundleDto>> GetMyLaundryBundlesAsync(string laundryAdminId)
        {
            var laundryRepo = _unitOfWork.Repository<Laundry, int>();
            var laundry = await laundryRepo.GetEntityWithSpecAsync(new LaundryByAdminIdSpec(laundryAdminId));
            if (laundry == null) return new List<BundleDto>();

            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();

            // جيب bundles الخاصة بالـ laundry
            var laundryBundles = await bundleRepo.GetAllWithSpecAsync(new BundlesByLaundryIdSpec(laundry.Id));

            // جيب Platform bundles كمان عشان تظهر للـ laundry admin
            var platformBundles = await bundleRepo.GetAllWithSpecAsync(new PlatformBundlesSpec());

            // ادمج الاتنين مع بعض
            var allBundles = laundryBundles
                .Concat(platformBundles)
                .DistinctBy(b => b.Id)
                .OrderBy(b => b.DisplayOrder)
                .ToList();

            return allBundles.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<BundleApprovalRequestDto>> GetMyApprovalRequestsAsync(string laundryAdminId)
        {
            var reqRepo = _unitOfWork.Repository<BundleApprovalRequest, Guid>();
            var requests = await reqRepo.GetAllWithSpecAsync(new BundleApprovalRequestsByLaundryAdminSpec(laundryAdminId));
            return requests.Select(MapToApprovalDto).ToList();
        }

        public async Task<IReadOnlyList<BundleDto>> GetBundlesByLaundryAsync(int laundryId)
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var bundles = await bundleRepo.GetAllWithSpecAsync(new ActiveBundlesByLaundrySpec(laundryId));

            var serviceRepo = _unitOfWork.Repository<LaundryDomain.Models.LaundryModules.Service, int>();
            var servicesSpec = new ServicesByLaundryIdSpec(laundryId);
            var laundryServices = await serviceRepo.GetAllWithSpecAsync(servicesSpec);

            var laundryCategories = laundryServices
                .Where(s => s.IsAvailable)
                .Select(s => s.Category)
                .Distinct()
                .ToList();

            var filteredBundles = new List<Bundle>();

            foreach (var bundle in bundles)
            {
                // 1. Must have items
                if (bundle.BundleItems == null || !bundle.BundleItems.Any())
                {
                    continue;
                }

                // 2. If platform bundle, laundry must support all allowed item categories
                if (!bundle.LaundryId.HasValue)
                {
                    bool satisfiesAll = true;
                    foreach (var item in bundle.BundleItems)
                    {
                        if (item.AllowedItemCategory.HasValue && !laundryCategories.Contains(item.AllowedItemCategory.Value))
                        {
                            satisfiesAll = false;
                            break;
                        }
                    }
                    if (!satisfiesAll)
                    {
                        continue;
                    }
                }
                else
                {
                    // If laundry-specific, verify all items reference services that belong to this laundry and are available
                    bool satisfiesAll = true;
                    foreach (var item in bundle.BundleItems)
                    {
                        var exists = laundryServices.Any(s => s.Id == item.ServiceId && s.IsAvailable);
                        if (!exists)
                        {
                            satisfiesAll = false;
                            break;
                        }
                    }
                    if (!satisfiesAll)
                    {
                        continue;
                    }
                }

                filteredBundles.Add(bundle);
            }

            return filteredBundles.Select(MapToDto).ToList();
        }

        public async Task<BundleDto?> GetBundleByIdAsync(Guid bundleId)
        {
            var repo = _unitOfWork.Repository<Bundle, Guid>();
            var bundle = await repo.GetEntityWithSpecAsync(new BundleByIdWithItemsSpec(bundleId));
            return bundle == null ? null : MapToDto(bundle);
        }

        public async Task<Pagination<BundleDto>> GetAllBundlesAsync(BundleSearchParams searchParams)
        {
            var repo = _unitOfWork.Repository<Bundle, Guid>();
            var all = await repo.GetAllWithSpecAsync(new AllBundlesSpec(searchParams));
            
            var mapped = all.Select(MapToDto).ToList();
            
            var countRepo = _unitOfWork.Repository<Bundle, Guid>();
            int count = await countRepo.CountAsync(new AllBundlesCountSpec(searchParams));
            
            return new Pagination<BundleDto>(searchParams.PageIndex, searchParams.PageSize, count, mapped);
        }

        public async Task ExpireOverdueBundlesAsync()
        {
            var bundleRepo = _unitOfWork.Repository<Bundle, Guid>();
            var expiredBundles = await bundleRepo.GetAllWithSpecAsync(new ExpiredBundlesSpec());
            
            foreach (var bundle in expiredBundles)
            {
                bundle.Status = BundleStatus.Expired;
                bundle.UpdatedAt = DateTime.UtcNow;
                bundleRepo.Update(bundle);
            }

            int count = expiredBundles.Count;
            if (count > 0)
            {
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("{count} bundles marked as expired", count);
            }
        }

        // ================== MAPPERS ==================

        private BundleDto MapToDto(Bundle bundle)
        {
            return new BundleDto
            {
                Id = bundle.Id,
                Name = bundle.Name,
                Description = bundle.Description,
                BundlePrice = bundle.BundlePrice,
                OriginalPrice = bundle.OriginalPrice,
                Status = bundle.Status,
                OwnerType = bundle.OwnerType,
                LaundryId = bundle.LaundryId,
                StartDate = bundle.StartDate,
                ExpiryDate = bundle.ExpiryDate,
                ImageUrl = bundle.ImageUrl,
                DisplayOrder = bundle.DisplayOrder,
                CreatedByAdminId = bundle.CreatedByAdminId,
                CreatedAt = bundle.CreatedAt,
                BundleItems = bundle.BundleItems?.Select(i => new BundleItemDto
                {
                    Id = i.Id,
                    ServiceId = i.ServiceId,
                    ServiceName = i.Service?.ServiceName ?? "Unknown", 
                    Quantity = i.Quantity,
                    AllowedItemCategory = i.AllowedItemCategory,
                    ServiceCategory = i.Service?.Category ?? ServiceCategory.Iron
                }).ToList() ?? new List<BundleItemDto>()
            };
        }

        private BundleApprovalRequestDto MapToApprovalDto(BundleApprovalRequest req)
        {
            return new BundleApprovalRequestDto
            {
                Id = req.Id,
                BundleId = req.BundleId,
                BundleName = req.Bundle?.Name ?? "Unknown",
                LaundryId = req.Bundle?.LaundryId,
                LaundryName = req.Bundle?.Laundry?.Name,
                RequestedByAdminId = req.RequestedByAdminId,
                RequestedByAdminName = req.RequestedByAdmin?.Name ?? "Unknown",
                Status = req.Status,
                RequestNote = req.RequestNote,
                ReviewNote = req.ReviewNote,
                RequestedAt = req.RequestedAt,
                ReviewedAt = req.ReviewedAt,
                IsEditRequest = req.IsEditRequest,
                PendingBundleSnapshot = req.PendingBundleSnapshot
            };
        }
    }
}
