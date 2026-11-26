using LaundryShared.Common;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.DTO.SuperAdmin;
using LaundryShared.DTO.SuperAdmin.LaundryShared.DTO.SuperAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.SuperAdmin
{
    /// <summary>
    /// عقد الخدمات الخاصة بالـ Super Admin
    /// بيحدد كل العمليات اللي يقدر يعملها على السيستم كله
    /// </summary>
    public interface ISuperAdminService
    {
        // ============================================================
        // إدارة المغاسل
        // ============================================================

        /// <summary>
        /// بيجيب كل المغاسل في السيستم
        /// سواء Active أو Inactive
        /// </summary>
        Task<Result<List<LaundryApprovalDto>>> GetAllLaundriesAsync();

        /// <summary>
        /// بيجيب تفاصيل مغسلة معينة
        /// </summary>
        Task<Result<LaundryApprovalDto>> GetLaundryByIdAsync(int laundryId);

        /// <summary>
        /// بيوافق على مغسلة جديدة أو يوقفها
        /// Active   = موافق عليها
        /// Inactive = موقوفة
        /// </summary>
        Task<Result<bool>> UpdateLaundryStatusAsync(int laundryId, UpdateLaundryStatusRequest request);

        /// <summary>
        /// SuperAdmin يقدر يضيف مغسلة مباشرة بدون شروط موافقات
        /// </summary>
        Task<Result<int>> AddLaundryDirectAsync(string superAdminId, SetupLaundryRequest request);

        // ============================================================
        // إدارة اليوزرز
        // ============================================================

        /// <summary>
        /// بيجيب كل اليوزرز في السيستم
        /// Customers + Couriers + Laundry Admins
        /// </summary>
        Task<Result<List<UserManagementDto>>> GetAllUsersAsync();

        /// <summary>
        /// بيجيب تفاصيل يوزر معين
        /// </summary>
        Task<Result<UserManagementDto>> GetUserByIdAsync(string userId);

        /// <summary>
        /// بيوقف أو يفعّل أكونت يوزر
        /// </summary>
        Task<Result<bool>> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request);

        // ============================================================
        // Laundry Admins Verifications
        // ============================================================
        Task<Result<List<LaundryAdminVerificationDto>>> GetPendingLaundryAdminsAsync(string? searchTerm = null);

        Task<Result<bool>> ReviewLaundryAdminAsync(string userId, bool isApproved, string? rejectionReason = null);

        // ============================================================
        // Commissions Oversight
        // ============================================================
        Task<Result<SystemCommissionsDto>> GetCommissionsOverviewAsync();

        // ============================================================
        // Coupons
        // ============================================================
        Task<Result<CouponDto>> CreateCouponAsync(CreateCouponRequest request);

        // ============================================================
        // Financial & Billing Oversight
        // ============================================================
        Task<Result<LaundryBillingInfoDto>> GetLaundryBillingInfoAsync(int laundryId);
        Task<Result<LaundryBillingInfoDto>> GetLaundryBillingInfoByAdminAsync(string adminId);
    }
}
