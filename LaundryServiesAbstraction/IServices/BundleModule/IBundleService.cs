using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaundryShared.Common;
using LaundryShared.DTO.BundleModule;
using LaundryShared.DTO.Order;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryServiesAbstraction.IServices.BundleModule
{
    public interface IBundleService
    {
        // ── SuperAdmin methods ──
        Task<Pagination<BundleDto>> GetAllBundlesAsync(BundleSearchParams searchParams);
        Task<Result<BundleDto>> CreateBundleAsync(CreateBundleRequest request, string adminId);
        Task<Result<BundleDto>> UpdateBundleAsync(Guid bundleId, UpdateBundleRequest request, string adminId);
        Task<Result> SetBundleStatusAsync(Guid bundleId, BundleStatus newStatus, string adminId);
        Task<Pagination<BundleApprovalRequestDto>> GetPendingApprovalsAsync(int pageIndex, int pageSize);
        Task<Result> ReviewBundleApprovalAsync(ReviewBundleApprovalRequest request, string superAdminId);

        // ── LaundryAdmin methods ──
        Task<Result<BundleApprovalRequestDto>> ProposeBundleAsync(ProposeBundleRequest request, string laundryAdminId);
        Task<Result<BundleApprovalRequestDto>> ProposeEditBundleAsync(ProposeEditBundleRequest request, string laundryAdminId);
        Task<Result> DeactivateBundleAsync(Guid bundleId, string adminId);
        Task<IReadOnlyList<BundleDto>> GetMyLaundryBundlesAsync(string laundryAdminId);
        Task<IReadOnlyList<BundleApprovalRequestDto>> GetMyApprovalRequestsAsync(string laundryAdminId);

        // ── Public/Customer methods ──
        Task<IReadOnlyList<BundleDto>> GetBundlesByLaundryAsync(int laundryId);
        Task<BundleDto?> GetBundleByIdAsync(Guid bundleId);

        // ── BackgroundService method ──
        Task ExpireOverdueBundlesAsync();
    }
}
