using LaundryServiesAbstraction.IServices.SuperAdmin;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.DTO.SuperAdmin;
using LaundryShared.DTO.SuperAdmin.LaundryShared.DTO.SuperAdmin;
using LaundryShared.DTO.Complaint;
using LaundryPerstince.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums.WalletEnums;
using LaundryShared.DTO.LaundryAdmin;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.Wallet;

namespace LaundryApi.Controllers
{
    /// <summary>
    /// البوابة بتاعة الـ Super Admin
    /// كل الـ Endpoints هنا محتاجة JWT Token بصلاحية SuperAdmin بس
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private static readonly TimeSpan FullDayOpenTime = TimeSpan.Zero;
        private static readonly TimeSpan FullDayCloseTime = new(23, 59, 59);

        private readonly ISuperAdminService _superAdminService;
        private readonly NadeefDbContext _dbContext;






        private string GetAdminId() => User.FindFirstValue("uid") ?? string.Empty;

        public SuperAdminController(ISuperAdminService superAdminService, NadeefDbContext dbContext)
        {
            _superAdminService = superAdminService;
            _dbContext = dbContext;
        }

        // ============================================================
        // إدارة المغاسل
        // ============================================================

        /// <summary>
        /// GET /api/admin/laundries
        /// بيرجع كل المغاسل في السيستم
        /// </summary>
        [HttpGet("laundries")]
        public async Task<IActionResult> GetAllLaundries()
        {
            var result = await _superAdminService.GetAllLaundriesAsync();

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ============================================================
        // Coupons
        // ============================================================

        [HttpPost("coupons")]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _superAdminService.CreateCouponAsync(request);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/admin/laundries/{id}
        /// بيرجع تفاصيل مغسلة معينة
        /// </summary>
        [HttpGet("laundries/{id}")]
        public async Task<IActionResult> GetLaundryById(int id)
        {
            var result = await _superAdminService.GetLaundryByIdAsync(id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// PUT /api/admin/laundries/{id}/status
        /// بيوافق على مغسلة أو يوقفها
        /// </summary>
        [HttpPut("laundries/{id}/status")]
        public async Task<IActionResult> UpdateLaundryStatus(
            int id, [FromBody] UpdateLaundryStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _superAdminService.UpdateLaundryStatusAsync(id, request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry status updated successfully" });
        }

        /// <summary>
        /// POST /api/admin/laundries
        /// إضافة مغسلة مباشرة بواسطة SuperAdmin بدون شروط موافقة
        /// </summary>
        [HttpPost("laundries")]
        public async Task<IActionResult> AddLaundryDirect([FromBody] SetupLaundryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _superAdminService.AddLaundryDirectAsync(GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry created successfully", LaundryId = result.Value });
        }

        // ============================================================
        // إدارة اليوزرز
        // ============================================================

        /// <summary>
        /// GET /api/admin/users
        /// بيرجع كل اليوزرز في السيستم
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _superAdminService.GetAllUsersAsync();

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/admin/users/{id}
        /// بيرجع تفاصيل يوزر معين
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var result = await _superAdminService.GetUserByIdAsync(id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// PUT /api/admin/users/{id}/status
        /// بيوقف أو يفعّل أكونت يوزر
        /// </summary>
        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(
            string id, [FromBody] UpdateUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _superAdminService.UpdateUserStatusAsync(id, request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "User status updated successfully" });
        }

        // ============================================================
        // Laundry Admins Verification
        // ============================================================

        [HttpGet("laundry-admins-verifications")]
        public async Task<IActionResult> GetLaundryAdminsVerifications([FromQuery] string? search = null)
        {
            var result = await _superAdminService.GetPendingLaundryAdminsAsync(search);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("laundry-admins-verifications/{userId}/approve")]
        public async Task<IActionResult> ApproveLaundryAdmin(string userId)
        {
            var result = await _superAdminService.ReviewLaundryAdminAsync(userId, true);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "LaundryAdmin has been approved and activated." });
        }

        [HttpPost("laundry-admins-verifications/{userId}/reject")]
        public async Task<IActionResult> RejectLaundryAdmin(string userId, [FromBody] ReviewVerificationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return BadRequest(new { Message = "Rejection reason is required." });

            var result = await _superAdminService.ReviewLaundryAdminAsync(userId, false, request.RejectionReason);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "LaundryAdmin has been rejected with the provided reason." });
        }

        // ============================================================
        // Commissions
        // ============================================================

        /// <summary>
        /// GET /api/admin/commissions
        /// بيرجع كل تفاصيل العمولات للمدير
        /// </summary>
        [HttpGet("commissions")]
        public async Task<IActionResult> GetCommissionsOverview()
        {
            var result = await _superAdminService.GetCommissionsOverviewAsync();

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaints()
        {
            var data = await _dbContext.Complaints
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ComplaintDto
                {
                    Id = c.Id,
                    OrderId = c.OrderId,
                    LaundryId = c.LaundryId,
                    LaundryName = _dbContext.Laundries.Where(l => l.Id == c.LaundryId).Select(l => l.Name).FirstOrDefault() ?? "",
                    CustomerId = c.CustomerId,
                    CustomerName = _dbContext.Users.Where(u => u.Id == c.CustomerId).Select(u => u.Name).FirstOrDefault() ?? "",
                    CustomerPhone = _dbContext.Users.Where(u => u.Id == c.CustomerId).Select(u => u.PhoneNumber).FirstOrDefault() ?? "",
                    CustomerEmail = _dbContext.Users.Where(u => u.Id == c.CustomerId).Select(u => u.Email).FirstOrDefault() ?? "",
                    Details = c.Details,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    ResolvedAt = c.ResolvedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("laundries/commission-status")]
        public async Task<IActionResult> GetLaundriesCommissionStatus()
        {
            const decimal commissionRate = 0.10m;

            var laundries = await _dbContext.Laundries
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.AdminId
                })
                .ToListAsync();

            var adminIds = laundries.Select(l => l.AdminId).Distinct().ToList();
            var admins = await _dbContext.Users
                .Where(u => adminIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.PhoneNumber })
                .ToListAsync();

            var deliveredOrders = await _dbContext.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var payments = await _dbContext.CommissionPayments
                .ToListAsync();

            var data = laundries.Select(l =>
            {
                var admin = admins.FirstOrDefault(a => a.Id == l.AdminId);
                var totalRevenue = deliveredOrders.Where(o => o.LaundryId == l.Id).Sum(o => o.TotalPrice);
                var commissionPaid = payments.Where(p => p.LaundryId == l.Id && p.Status == CommissionPaymentStatus.Paid).Sum(p => p.Amount);
                var commissionDue = Math.Max((totalRevenue * commissionRate) - commissionPaid, 0m);
                var pendingCount = payments.Count(p => p.LaundryId == l.Id && p.Status == CommissionPaymentStatus.Pending);

                return new LaundryCommissionStatusDto
                {
                    LaundryId = l.Id,
                    LaundryName = l.Name,
                    AdminId = l.AdminId,
                    AdminName = admin?.Name ?? string.Empty,
                    AdminPhone = admin?.PhoneNumber ?? string.Empty,
                    TotalRevenue = totalRevenue,
                    CommissionRate = commissionRate * 100,
                    CommissionPaid = commissionPaid,
                    CommissionDue = commissionDue,
                    Status = commissionDue <= 0 ? "Paid" : "Due",
                    PendingPaymentsCount = pendingCount
                };
            }).ToList();

            return Ok(data);
        }

        [HttpPut("laundries/{id}/freeze")]
        public async Task<IActionResult> FreezeLaundry(int id, [FromBody] FreezeLaundryRequest request)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            laundry.Status = LaundryStatus.Inactive;
            laundry.Availability = LaundryAvailability.Closed;
            laundry.UpdatedAt = DateTime.UtcNow;

            var wallet = await _dbContext.LaundryWallets.FirstOrDefaultAsync(w => w.LaundryId == id);
            if (wallet != null)
            {
                wallet.Status = LaundryWalletStatus.Suspended;
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Laundry account has been frozen", Reason = request?.Reason });
        }

        [HttpGet("laundries/{id}/availability/schedule")]
        public async Task<IActionResult> GetLaundrySchedule(int id)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var days = await _dbContext.LaundryAvailabilitySchedules
                .Where(s => s.LaundryId == id)
                .OrderBy(s => s.DayOfWeek)
                .Select(s => new AvailabilityScheduleDayDto
                {
                    DayOfWeek = s.DayOfWeek,
                    IsOpen = s.IsOpen
                })
                .ToListAsync();

            if (days.Count == 0)
            {
                days = Enumerable.Range(0, 7).Select(d => new AvailabilityScheduleDayDto
                {
                    DayOfWeek = d,
                    IsOpen = d != 0
                }).ToList();
            }

            return Ok(new AvailabilityScheduleDto { Days = days });
        }

        [HttpPut("laundries/{id}/availability/schedule")]
        public async Task<IActionResult> UpdateLaundrySchedule(int id, [FromBody] UpdateAvailabilityScheduleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!IsScheduleValid(request.Days, out var validationError))
                return BadRequest(new { Message = validationError });

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var existing = await _dbContext.LaundryAvailabilitySchedules
                .Where(s => s.LaundryId == id)
                .ToListAsync();

            _dbContext.LaundryAvailabilitySchedules.RemoveRange(existing);

            var updated = request.Days.Select(d => new LaundryAvailabilitySchedule
            {
                LaundryId = id,
                DayOfWeek = d.DayOfWeek,
                IsOpen = d.IsOpen,
                OpenTime = FullDayOpenTime,
                CloseTime = FullDayCloseTime,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _dbContext.LaundryAvailabilitySchedules.AddRangeAsync(updated);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Schedule updated successfully" });
        }

        [HttpGet("laundries/{id}/availability/capacity")]
        public async Task<IActionResult> GetLaundryCapacity(int id)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var settings = await _dbContext.LaundryCapacitySettings
                .FirstOrDefaultAsync(c => c.LaundryId == id);

            if (settings == null)
            {
                settings = new LaundryCapacitySetting { LaundryId = id, CreatedAt = DateTime.UtcNow };
                await _dbContext.LaundryCapacitySettings.AddAsync(settings);
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new CapacitySettingsDto
            {
                MaxOrdersPerDay = settings.MaxOrdersPerDay,
                LeadTimeHours = settings.LeadTimeHours
            });
        }

        [HttpPut("laundries/{id}/availability/capacity")]
        public async Task<IActionResult> UpdateLaundryCapacity(int id, [FromBody] CapacitySettingsDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var settings = await _dbContext.LaundryCapacitySettings
                .FirstOrDefaultAsync(c => c.LaundryId == id);

            if (settings == null)
            {
                settings = new LaundryCapacitySetting { LaundryId = id, CreatedAt = DateTime.UtcNow };
                await _dbContext.LaundryCapacitySettings.AddAsync(settings);
            }

            settings.MaxOrdersPerDay = request.MaxOrdersPerDay;
            settings.LeadTimeHours = request.LeadTimeHours;
            settings.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Capacity settings updated successfully" });
        }

        [HttpGet("laundries/{id}/availability/closed-dates")]
        public async Task<IActionResult> GetLaundryClosedDates(int id)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var dates = await _dbContext.LaundryClosedDates
                .Where(d => d.LaundryId == id)
                .OrderBy(d => d.Date)
                .Select(d => new ClosedDateDto
                {
                    Id = d.Id,
                    Date = d.Date,
                    Reason = d.Reason
                })
                .ToListAsync();

            return Ok(dates);
        }

        [HttpPost("laundries/{id}/availability/closed-dates")]
        public async Task<IActionResult> AddLaundryClosedDate(int id, [FromBody] UpdateClosedDateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var entity = new LaundryClosedDate
            {
                LaundryId = id,
                Date = request.Date.Date,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.LaundryClosedDates.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Closed date added successfully", Id = entity.Id });
        }

        [HttpDelete("laundries/{id}/availability/closed-dates/{closedDateId}")]
        public async Task<IActionResult> RemoveLaundryClosedDate(int id, int closedDateId)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == id);
            if (laundry == null)
                return BadRequest(new { Message = "Laundry not found" });

            var entity = await _dbContext.LaundryClosedDates
                .FirstOrDefaultAsync(d => d.Id == closedDateId && d.LaundryId == id);

            if (entity == null)
                return BadRequest(new { Message = "Closed date not found" });

            _dbContext.LaundryClosedDates.Remove(entity);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Closed date removed successfully" });
        }

        private async Task EnsureDefaultScheduleAsync(int laundryId)
        {
            var exists = await _dbContext.LaundryAvailabilitySchedules.AnyAsync(s => s.LaundryId == laundryId);
            if (exists) return;

            var days = Enumerable.Range(0, 7).Select(d => new LaundryAvailabilitySchedule
            {
                LaundryId = laundryId,
                DayOfWeek = d,
                IsOpen = d != 0,
                OpenTime = FullDayOpenTime,
                CloseTime = FullDayCloseTime,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.LaundryAvailabilitySchedules.AddRangeAsync(days);
            await _dbContext.SaveChangesAsync();
        }

        private static bool IsScheduleValid(List<AvailabilityScheduleDayDto> days, out string error)
        {
            error = string.Empty;

            if (days == null || days.Count == 0)
            {
                error = "Schedule cannot be empty.";
                return false;
            }

            if (days.Any(d => d.DayOfWeek < 0 || d.DayOfWeek > 6))
            {
                error = "DayOfWeek must be between 0 and 6.";
                return false;
            }

            if (days.GroupBy(d => d.DayOfWeek).Any(g => g.Count() > 1))
            {
                error = "Duplicate days are not allowed in schedule.";
                return false;
            }

            return true;
        }

        // ============================================================
        // Financial & Billing Oversight
        // ============================================================

        /// <summary>
        /// GET /api/admin/laundries/{laundryId}/billing
        /// </summary>
        [HttpGet("laundries/{laundryId}/billing")]
        public async Task<IActionResult> GetLaundryBillingInfo(int laundryId)
        {
            var result = await _superAdminService.GetLaundryBillingInfoAsync(laundryId);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/admin/users/{userId}/laundry-billing
        /// </summary>
        [HttpGet("users/{userId}/laundry-billing")]
        public async Task<IActionResult> GetUserLaundryBillingInfo(string userId)
        {
            var result = await _superAdminService.GetLaundryBillingInfoByAdminAsync(userId);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/admin/laundries/{laundryId}/payout-profile
        /// Retrieves the payout profile for a specific laundry (SuperAdmin only).
        /// </summary>
        [HttpGet("laundries/{laundryId}/payout-profile")]
        public async Task<IActionResult> GetPayoutProfile(int laundryId)
        {
            var laundry = await _dbContext.Laundries
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == laundryId);

            if (laundry == null)
                return NotFound(new { Message = "Laundry not found." });

            var payoutProfile = await _dbContext.LaundryPayoutProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LaundryId == laundryId);

            if (payoutProfile == null)
            {
                return Ok(new PayoutProfileDto
                {
                    Id = 0,
                    LaundryId = laundryId,
                    RecipientFullName = string.Empty
                });
            }

            var dto = new PayoutProfileDto
            {
                Id = payoutProfile.Id,
                LaundryId = payoutProfile.LaundryId,
                TransferMethod = payoutProfile.TransferMethod,
                TransferType = payoutProfile.TransferType,
                RecipientFullName = payoutProfile.RecipientFullName,
                RecipientMobileNumber = payoutProfile.RecipientMobileNumber,
                BankName = payoutProfile.BankName,
                BankAccountNumber = payoutProfile.BankAccountNumber,
                CardNumber = payoutProfile.CardNumber,
                NationalId = payoutProfile.NationalId
            };

            return Ok(dto);
        }

        /// <summary>
        /// POST /api/admin/laundries/{laundryId}/payout-profile
        /// Creates or updates the payout profile for a specific laundry (SuperAdmin only).
        /// </summary>
        [HttpPost("laundries/{laundryId}/payout-profile")]
        public async Task<IActionResult> UpsertPayoutProfile(int laundryId, [FromBody] UpsertPayoutProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == laundryId);
            if (laundry == null)
                return NotFound(new { Message = "Laundry not found." });

            // Specific Validation per TransferMethod
            if (request.TransferMethod == PayoutTransferMethod.BankAccount)
            {
                if (string.IsNullOrWhiteSpace(request.BankName))
                    return BadRequest(new { Message = "Bank Name is required for Bank Account transfer method." });
                if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
                    return BadRequest(new { Message = "Bank Account Number is required for Bank Account transfer method." });
            }
            else if (request.TransferMethod == PayoutTransferMethod.MobileWallet)
            {
                if (string.IsNullOrWhiteSpace(request.RecipientMobileNumber))
                    return BadRequest(new { Message = "Recipient Mobile Number is required for Mobile Wallet transfer method." });
                if (!request.TransferType.HasValue)
                    return BadRequest(new { Message = "Transfer Type (Standard/Instant) is required for Mobile Wallet transfer method." });
            }
            else if (request.TransferMethod == PayoutTransferMethod.Card)
            {
                if (string.IsNullOrWhiteSpace(request.CardNumber))
                    return BadRequest(new { Message = "Card Number is required for Card transfer method." });
                if (string.IsNullOrWhiteSpace(request.BankName))
                    return BadRequest(new { Message = "Bank Name is required for Card transfer method." });
            }
            else if (request.TransferMethod == PayoutTransferMethod.OctoCard)
            {
                if (string.IsNullOrWhiteSpace(request.CardNumber))
                    return BadRequest(new { Message = "Card Number is required for Octo Card transfer method." });
            }

            var payoutProfile = await _dbContext.LaundryPayoutProfiles
                .FirstOrDefaultAsync(p => p.LaundryId == laundryId);

            if (payoutProfile == null)
            {
                payoutProfile = new LaundryPayoutProfile
                {
                    LaundryId = laundryId,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.LaundryPayoutProfiles.Add(payoutProfile);
            }
            else
            {
                payoutProfile.UpdatedAt = DateTime.UtcNow;
            }

            payoutProfile.TransferMethod = request.TransferMethod;
            payoutProfile.TransferType = request.TransferType;
            payoutProfile.RecipientFullName = request.RecipientFullName;
            payoutProfile.RecipientMobileNumber = request.RecipientMobileNumber;
            payoutProfile.BankName = request.BankName;
            payoutProfile.BankAccountNumber = request.BankAccountNumber;
            payoutProfile.CardNumber = request.CardNumber;
            payoutProfile.NationalId = request.NationalId;

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Payout profile updated successfully." });
        }

        /// <summary>
        /// POST /api/admin/laundries/{laundryId}/payouts/process
        /// </summary>
        [HttpPost("laundries/{laundryId}/payouts/process")]
        public async Task<IActionResult> ProcessPayout(int laundryId, [FromBody] ProcessPayoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wallet = await _dbContext.LaundryWallets.FirstOrDefaultAsync(w => w.LaundryId == laundryId);
            if (wallet == null)
                return NotFound(new { Message = "Laundry wallet not found." });

            if (request.Amount <= 0)
                return BadRequest(new { Message = "Payout amount must be greater than zero." });

            if (request.Amount > wallet.AvailableBalance)
                return BadRequest(new { Message = "Payout amount cannot exceed available balance." });

            wallet.AvailableBalance -= request.Amount;
            wallet.LastPayoutDate = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            var payout = new LaundryPayout
            {
                LaundryId = laundryId,
                Amount = request.Amount,
                Status = "Completed",
                Method = request.Method,
                Reference = request.Reference,
                Notes = request.Notes,
                CreatedBy = "SuperAdmin",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.LaundryPayouts.AddAsync(payout);

            var walletTx = new WalletTransaction
            {
                LaundryWalletId = wallet.Id,
                Amount = request.Amount,
                CommissionRate = 0,
                CommissionAmount = 0,
                Type = TransactionType.Debit,
                Source = TransactionSource.Payout,
                Status = TransactionStatus.Completed,
                PaymobTransactionId = request.Reference,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.WalletTransactions.AddAsync(walletTx);
            await _dbContext.SaveChangesAsync();

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == laundryId);
            if (laundry != null)
            {
                var notificationService = (LaundryServiesAbstraction.IServices.Notifications.INotificationService?)
                    HttpContext.RequestServices.GetService(typeof(LaundryServiesAbstraction.IServices.Notifications.INotificationService));
                if (notificationService != null)
                {
                    try
                    {
                        await notificationService.SendNotificationAsync(
                            laundry.AdminId,
                            "Payout Received",
                            $"A payout of {request.Amount:F2} EGP has been transferred to your account."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send payout notification: {ex.Message}");
                    }
                }
            }

            return Ok(new
            {
                Success = true,
                PayoutId = payout.Id.ToString(),
                ProcessedAmount = request.Amount,
                RemainingAvailableBalance = wallet.AvailableBalance,
                ProcessedAt = payout.CreatedAt,
                Message = "Payout processed successfully."
            });
        }

        /// <summary>
        /// POST /api/admin/laundries/{laundryId}/payouts/reminder
        /// </summary>
        [HttpPost("laundries/{laundryId}/payouts/reminder")]
        public async Task<IActionResult> SendPayoutReminder(int laundryId)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.Id == laundryId);
            if (laundry == null)
                return NotFound(new { Message = "Laundry not found." });

            var wallet = await _dbContext.LaundryWallets.FirstOrDefaultAsync(w => w.LaundryId == laundryId);
            var availableBalance = wallet?.AvailableBalance ?? 0m;

            var payoutProfile = await _dbContext.LaundryPayoutProfiles.FirstOrDefaultAsync(p => p.LaundryId == laundryId);

            string title = "Payout Action Required";
            string message = "";

            if (payoutProfile == null || payoutProfile.TransferMethod == 0 || string.IsNullOrEmpty(payoutProfile.RecipientFullName))
            {
                title = "Payout Profile Setup Required";
                message = "Please set up your payout profile in your account settings so we can process your payouts.";
            }
            else
            {
                message = $"You have an available balance of {availableBalance:F2} EGP ready for payout. Please make sure your payout details are up to date.";
            }

            var notificationService = (LaundryServiesAbstraction.IServices.Notifications.INotificationService?)
                HttpContext.RequestServices.GetService(typeof(LaundryServiesAbstraction.IServices.Notifications.INotificationService));
            if (notificationService == null)
                return BadRequest(new { Message = "Notification service is not available." });

            await notificationService.SendNotificationAsync(laundry.AdminId, title, message);

            return Ok(new
            {
                Success = true,
                Message = "Reminder sent successfully."
            });
        }

        /// <summary>
        /// GET /api/admin/laundries/{laundryId}/payouts
        /// </summary>
        [HttpGet("laundries/{laundryId}/payouts")]
        public async Task<IActionResult> GetLaundryPayouts(int laundryId)
        {
            var payouts = await _dbContext.LaundryPayouts
                .Where(p => p.LaundryId == laundryId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Status = p.Status,
                    Method = p.Method,
                    Reference = p.Reference,
                    Notes = p.Notes,
                    ProcessedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy
                })
                .ToListAsync();

            return Ok(payouts);
        }
    }
}
