using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.Wallet;
using LaundryServiesAbstraction.IServices.SuperAdmin;
using LaundryShared.Common;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.DTO.SuperAdmin;
using LaundryShared.DTO.SuperAdmin.LaundryShared.DTO.SuperAdmin;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaundryDomain.Specifications.LaundrySpecs;
using LaundryDomain.Specifications.OrderSpecs;
using LaundryDomain.Specifications.WalletSpecs;
using LaundryDomain.Specifications.LaundrySpecsandwalletSpecs;
using LaundryDomain.Specifications.LaundryAdminIdSpec;

namespace LaundryServies.Services.SuperAdmin
{
    /// <summary>
    /// ده السيرفس بتاع الـ Super Admin
    /// بيتحكم في كل المغاسل وكل اليوزرز في السيستم
    /// </summary>
    public class SuperAdminService : ISuperAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperAdminService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ============================================================
        // إدارة المغاسل
        // ============================================================

        public async Task<Result<List<LaundryApprovalDto>>> GetAllLaundriesAsync()
        {
            // بنجيب كل المغاسل بغض النظر عن حالتها
            var laundries = await _unitOfWork.Repository<Laundry, int>()
                .GetAllWithSpecAsync(new AllLaundriesSpec());

            var result = laundries.Select(l => new LaundryApprovalDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                ImageUrl = l.ImageUrl,
                Status = l.Status,
                Availability = l.Availability,
                AverageRating = l.AverageRating,
                CreatedAt = l.CreatedAt
            }).ToList();

            return Result<List<LaundryApprovalDto>>.Success(result);
        }

        public async Task<Result<LaundryApprovalDto>> GetLaundryByIdAsync(int laundryId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetByIdAsync(laundryId);

            if (laundry is null)
                return Result<LaundryApprovalDto>.Failure("Laundry not found");

            var dto = new LaundryApprovalDto
            {
                Id = laundry.Id,
                Name = laundry.Name,
                Address = laundry.Address,
                ImageUrl = laundry.ImageUrl,
                Status = laundry.Status,
                Availability = laundry.Availability,
                AverageRating = laundry.AverageRating,
                CreatedAt = laundry.CreatedAt
            };

            return Result<LaundryApprovalDto>.Success(dto);
        }

        public async Task<Result<bool>> UpdateLaundryStatusAsync(int laundryId, UpdateLaundryStatusRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetByIdAsync(laundryId);

            if (laundry is null)
                return Result<bool>.Failure("Laundry not found");

            // لو بيوقف المغسلة لازم يكتب سبب
            if (request.Status == LaundryShared.Enums.CustomerEnums.LaundryStatus.Inactive
                && string.IsNullOrWhiteSpace(request.Reason))
                return Result<bool>.Failure("Reason is required when suspending a laundry");

            laundry.Status = request.Status;
            laundry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Laundry, int>().Update(laundry);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<int>> AddLaundryDirectAsync(string superAdminId, SetupLaundryRequest request)
        {
            var superAdmin = await _userManager.FindByIdAsync(superAdminId);
            if (superAdmin == null)
                return Result<int>.Failure("Super admin not found");

            var laundry = new Laundry
            {
                Name = request.Name,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AdminId = superAdminId,
                Status = LaundryShared.Enums.CustomerEnums.LaundryStatus.Active,
                Availability = LaundryShared.Enums.CustomerEnums.LaundryAvailability.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Laundry, int>().AddAsync(laundry);
            await _unitOfWork.CompleteAsync();

            return Result<int>.Success(laundry.Id);
        }

        // ============================================================
        // إدارة اليوزرز
        // ============================================================

        public async Task<Result<List<UserManagementDto>>> GetAllUsersAsync()
        {
            // بنجيب كل اليوزرز ما عدا الـ Super Admins
            var users = _userManager.Users
                .Where(u => u.Role != LaundryShared.Enums.UserRole.SuperAdmin)
                .ToList();

            var result = users.Select(u => new UserManagementDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Result<List<UserManagementDto>>.Success(result);
        }

        public async Task<Result<UserManagementDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return Result<UserManagementDto>.Failure("User not found");

            var dto = new UserManagementDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return Result<UserManagementDto>.Success(dto);
        }

        public async Task<Result<bool>> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return Result<bool>.Failure("User not found");

            // لو بيوقف الأكونت لازم يكتب سبب
            if (!request.IsActive && string.IsNullOrWhiteSpace(request.Reason))
                return Result<bool>.Failure("Reason is required when suspending a user");

            user.IsActive = request.IsActive;

            await _userManager.UpdateAsync(user);

            return Result<bool>.Success(true);
        }

        // ============================================================
        // Laundry Admins Verifications
        // ============================================================

        public async Task<Result<List<LaundryAdminVerificationDto>>> GetPendingLaundryAdminsAsync(string? searchTerm = null)
        {
            var query = _userManager.Users.Where(u => u.Role == LaundryShared.Enums.UserRole.LaundryAdmin);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || (u.Email != null && u.Email.Contains(searchTerm)));
            }

            var users = query.OrderByDescending(u => u.CreatedAt).ToList();

            var dtos = users.Select(u => new LaundryAdminVerificationDto
            {
                UserId = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber ?? "",
                IsIdentityVerified = u.IsIdentityVerified,
                IdCardImageUrl = u.IdCardImageUrl,
                CommercialRegisterDocumentUrl = u.CommercialRegisterDocumentUrl,
                AdminApprovalStatus = u.AdminApprovalStatus,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Result<List<LaundryAdminVerificationDto>>.Success(dtos);
        }

        public async Task<Result<bool>> ReviewLaundryAdminAsync(string userId, bool isApproved, string? rejectionReason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != LaundryShared.Enums.UserRole.LaundryAdmin)
                return Result<bool>.Failure("Invalid LaundryAdmin user.");

            if (isApproved)
            {
                user.AdminApprovalStatus = LaundryShared.Enums.LaundryAdminApprovalStatus.Approved;
                user.IsActive = true; 
                user.AdminRejectionReason = null;

                // Update associated Laundry status to Active and availability to Open
                var laundry = await _unitOfWork.Repository<Laundry, int>()
                    .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(userId));

                if (laundry != null)
                {
                    laundry.Status = LaundryShared.Enums.CustomerEnums.LaundryStatus.Active;
                    laundry.Availability = LaundryShared.Enums.CustomerEnums.LaundryAvailability.Open;
                    laundry.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<Laundry, int>().Update(laundry);
                    await _unitOfWork.CompleteAsync();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                    return Result<bool>.Failure("Rejection reason is required.");

                user.AdminApprovalStatus = LaundryShared.Enums.LaundryAdminApprovalStatus.Rejected;
                user.IsActive = false; // They can't login or use the app functionality, or maybe they can just check status
                user.AdminRejectionReason = rejectionReason;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result<bool>.Failure("Failed to update user status.");

            // Can trigger a notification/email to user here

            return Result<bool>.Success(true);
        }

        // ============================================================
        // Commissions Oversight
        // ============================================================

        public async Task<Result<SystemCommissionsDto>> GetCommissionsOverviewAsync()
        {
            var wallets = (await _unitOfWork.Repository<LaundryWallet, int>().GetAllAsync()).ToList();
            var allLaundries = (await _unitOfWork.Repository<Laundry, int>().GetAllAsync()).ToList();
            
            var commissionLogs = (await _unitOfWork.Repository<LaundryDomain.Models.Wallet.CommissionLog, int>().GetAllAsync()).ToList();

            var recentTransactions = commissionLogs
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .Select(c => new CommissionTransactionDto
                {
                    Id = c.Id,
                    LaundryName = allLaundries.FirstOrDefault(l => l.Id == c.LaundryId)?.Name ?? "Unknown",
                    OrderId = c.OrderId ?? 0,
                    OrderAmount = c.OrderAmount,
                    CommissionPercentage = c.CommissionPercentage,
                    CommissionAmount = c.CommissionAmount,
                    CreatedAt = c.CreatedAt
                }).ToList();

            var laundryDebts = wallets.Select(w => new LaundryDebtDto
            {
                LaundryId = w.LaundryId,
                LaundryName = allLaundries.FirstOrDefault(l => l.Id == w.LaundryId)?.Name ?? "Unknown",
                TotalEarnings = w.TotalEarnings,
                PendingCommission = w.PendingCommission,
                DebtCeiling = w.DebtCeiling,
                Status = w.Status.ToString()
            }).OrderByDescending(w => w.PendingCommission).ToList();

            var dto = new SystemCommissionsDto
            {
                TotalPlatformCommissions = commissionLogs.Sum(c => c.CommissionAmount),
                TotalPendingDebts = wallets.Sum(w => w.PendingCommission),
                RecentTransactions = recentTransactions,
                LaundryDebts = laundryDebts
            };

            return Result<SystemCommissionsDto>.Success(dto);
        }

        // ============================================================
        // Coupons
        // ============================================================
        public async Task<Result<CouponDto>> CreateCouponAsync(CreateCouponRequest request)
        {
            if (request.DiscountValue <= 0)
                return Result<CouponDto>.Failure("Discount value must be greater than 0.");

            if (request.IsPercentage && request.DiscountValue > 100)
                return Result<CouponDto>.Failure("Percentage discount cannot exceed 100.");

            if (request.ExpiryDate.HasValue && request.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
                return Result<CouponDto>.Failure("Expiry date must be in the future.");

            if (request.LaundryId.HasValue)
            {
                var laundry = await _unitOfWork.Repository<Laundry, int>().GetByIdAsync(request.LaundryId.Value);
                if (laundry == null)
                    return Result<CouponDto>.Failure("Laundry not found.");
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return Result<CouponDto>.Failure("Coupon code is required.");

            var existing = await _unitOfWork.Repository<Coupon, int>()
                .GetEntityWithSpecAsync(new CouponByCodeSpec(normalizedCode));
            if (existing != null)
                return Result<CouponDto>.Failure("Coupon code already exists.");

            var coupon = new Coupon
            {
                Code = normalizedCode,
                IsPercentage = request.IsPercentage,
                DiscountValue = request.DiscountValue,
                ExpiryDate = request.ExpiryDate,
                UsageLimit = request.UsageLimit,
                IsActive = request.IsActive,
                LaundryId = request.LaundryId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Coupon, int>().AddAsync(coupon);
            await _unitOfWork.CompleteAsync();

            return Result<CouponDto>.Success(new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                IsPercentage = coupon.IsPercentage,
                DiscountValue = coupon.DiscountValue,
                ExpiryDate = coupon.ExpiryDate,
                UsageLimit = coupon.UsageLimit,
                CurrentUsage = coupon.CurrentUsage,
                IsActive = coupon.IsActive,
                LaundryId = coupon.LaundryId,
                CreatedAt = coupon.CreatedAt
            });
        }

        // ============================================================
        // Financial & Billing Oversight
        // ============================================================
        public async Task<Result<LaundryBillingInfoDto>> GetLaundryBillingInfoAsync(int laundryId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>().GetByIdAsync(laundryId);
            if (laundry == null)
                return Result<LaundryBillingInfoDto>.Failure("Laundry not found.");

            var admin = await _userManager.FindByIdAsync(laundry.AdminId);
            var adminName = admin != null ? $"{admin.FirstName} {admin.LastName}".Trim() : "N/A";
            var adminEmail = admin?.Email ?? "N/A";

            var payoutProfile = await _unitOfWork.Repository<LaundryPayoutProfile, int>()
                .GetEntityWithSpecAsync(new LaundryPayoutProfileByLaundryIdSpec(laundryId));

            var wallet = await _unitOfWork.Repository<LaundryWallet, int>()
                .GetEntityWithSpecAsync(new LaundryWalletByLaundryIdSpec(laundryId));

            var dto = new LaundryBillingInfoDto
            {
                LaundryId = laundry.Id,
                LaundryName = laundry.Name,
                AdminName = adminName,
                AdminEmail = adminEmail,

                TransferMethod = payoutProfile?.TransferMethod.ToString() ?? "Not Configured",
                TransferType = payoutProfile?.TransferType?.ToString(),
                RecipientFullName = payoutProfile?.RecipientFullName ?? string.Empty,
                RecipientMobileNumber = payoutProfile?.RecipientMobileNumber,
                BankName = payoutProfile?.BankName,
                BankAccountNumber = payoutProfile?.BankAccountNumber,
                CardNumber = payoutProfile?.CardNumber,
                NationalId = payoutProfile?.NationalId,

                TotalEarnings = wallet?.TotalEarnings ?? 0m,
                AvailableBalance = wallet?.AvailableBalance ?? 0m,
                PayoutableAmount = wallet?.AvailableBalance ?? 0m,
                PendingCommission = wallet?.PendingCommission ?? 0m,
                DebtCeiling = wallet?.DebtCeiling ?? 200m,
                WalletStatus = wallet?.Status.ToString() ?? "Inactive",
                LastPayoutDate = wallet?.LastPayoutDate
            };

            return Result<LaundryBillingInfoDto>.Success(dto);
        }

        public async Task<Result<LaundryBillingInfoDto>> GetLaundryBillingInfoByAdminAsync(string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry == null)
                return Result<LaundryBillingInfoDto>.Failure("Laundry not found for the specified admin user.");

            var admin = await _userManager.FindByIdAsync(adminId);
            var adminName = admin != null ? $"{admin.FirstName} {admin.LastName}".Trim() : "N/A";
            var adminEmail = admin?.Email ?? "N/A";

            var payoutProfile = await _unitOfWork.Repository<LaundryPayoutProfile, int>()
                .GetEntityWithSpecAsync(new LaundryPayoutProfileByLaundryIdSpec(laundry.Id));

            var wallet = await _unitOfWork.Repository<LaundryWallet, int>()
                .GetEntityWithSpecAsync(new LaundryWalletByLaundryIdSpec(laundry.Id));

            var dto = new LaundryBillingInfoDto
            {
                LaundryId = laundry.Id,
                LaundryName = laundry.Name,
                AdminName = adminName,
                AdminEmail = adminEmail,

                TransferMethod = payoutProfile?.TransferMethod.ToString() ?? "Not Configured",
                TransferType = payoutProfile?.TransferType?.ToString(),
                RecipientFullName = payoutProfile?.RecipientFullName ?? string.Empty,
                RecipientMobileNumber = payoutProfile?.RecipientMobileNumber,
                BankName = payoutProfile?.BankName,
                BankAccountNumber = payoutProfile?.BankAccountNumber,
                CardNumber = payoutProfile?.CardNumber,
                NationalId = payoutProfile?.NationalId,

                TotalEarnings = wallet?.TotalEarnings ?? 0m,
                AvailableBalance = wallet?.AvailableBalance ?? 0m,
                PayoutableAmount = wallet?.AvailableBalance ?? 0m,
                PendingCommission = wallet?.PendingCommission ?? 0m,
                DebtCeiling = wallet?.DebtCeiling ?? 200m,
                WalletStatus = wallet?.Status.ToString() ?? "Inactive",
                LastPayoutDate = wallet?.LastPayoutDate
            };

            return Result<LaundryBillingInfoDto>.Success(dto);
        }
    }
}
