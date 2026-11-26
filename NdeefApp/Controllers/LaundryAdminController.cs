using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.LaundryAdmin;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.DTO.Complaint;
using LaundryShared.DTO.PaymentKashier;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.Wallet;
using LaundryShared.Enums.WalletEnums;
using LaundryPerstince.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Added for IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    /// <summary>
    /// Ø§Ù„Ø¨ÙˆØ§Ø¨Ø© Ø¨ØªØ§Ø¹Ø© ØµØ§Ø­Ø¨ Ø§Ù„Ù…ØºØ³Ù„Ø©
    /// ÙƒÙ„ Ø§Ù„Ù€ Endpoints Ù‡Ù†Ø§ Ù…Ø­ØªØ§Ø¬Ø© JWT Token Ø¨ØµÙ„Ø§Ø­ÙŠØ© LaundryAdmin
    /// </summary>
    [Route("api/laundry-admin")]
    [ApiController]
    [Authorize(Roles = "LaundryAdmin")]
    public class LaundryAdminController : ControllerBase
    {
        private static readonly TimeSpan FullDayOpenTime = TimeSpan.Zero;
        private static readonly TimeSpan FullDayCloseTime = new(23, 59, 59);

        private static readonly (string Name, ServiceCategory Category)[] ServiceCatalog =
        {
            ("قميص - Shirt", ServiceCategory.Iron),
            ("قميص - Shirt", ServiceCategory.DryCleaning),
            ("بنطلون - Pants", ServiceCategory.Iron),
            ("بنطلون - Pants", ServiceCategory.DryCleaning),
            ("بلوزة - Blouse", ServiceCategory.Iron),
            ("بلوزة - Blouse", ServiceCategory.DryCleaning),
            ("بدلة - Suit", ServiceCategory.Iron),
            ("بدلة - Suit", ServiceCategory.DryCleaning),
            ("بلوفر - Pullover", ServiceCategory.Iron),
            ("بلوفر - Pullover", ServiceCategory.DryCleaning),
            ("تيشيرت - T-Shirt", ServiceCategory.Iron),
            ("تيشيرت - T-Shirt", ServiceCategory.DryCleaning),
            ("جاكيت - Jacket", ServiceCategory.Iron),
            ("جاكيت - Jacket", ServiceCategory.DryCleaning),
            ("بالطو - Coat", ServiceCategory.Iron),
            ("بالطو - Coat", ServiceCategory.DryCleaning),
            ("جلابية - Galabiya", ServiceCategory.Iron),
            ("جلابية - Galabiya", ServiceCategory.DryCleaning),
            ("عباية - Abaya", ServiceCategory.Iron),
            ("عباية - Abaya", ServiceCategory.DryCleaning),
            ("تونك - Tunic", ServiceCategory.Iron),
            ("تونك - Tunic", ServiceCategory.DryCleaning),
            ("شورت - Shorts", ServiceCategory.Iron),
            ("شورت - Shorts", ServiceCategory.DryCleaning),
            ("قطعة أطفال - Kids Wear", ServiceCategory.Iron),
            ("قطعة أطفال - Kids Wear", ServiceCategory.DryCleaning),
            ("ملاية - Bed Sheet", ServiceCategory.Iron),
            ("ملاية - Bed Sheet", ServiceCategory.DryCleaning),
            ("سجاد - Carpet", ServiceCategory.DryCleaning),
            ("بطانية دبل - Double Blanket", ServiceCategory.DryCleaning),
            ("بطانية عادية - Regular Blanket", ServiceCategory.DryCleaning),
            ("لحاف كبير - Large Quilt", ServiceCategory.DryCleaning),
            ("لحاف صغير - Small Quilt", ServiceCategory.DryCleaning),
            ("غسيل عادي الكيلو - Washing per Kg", ServiceCategory.Wash),
            ("فستان زفاف - Wedding Dress", ServiceCategory.Specialty),
            ("ستائر - Curtains", ServiceCategory.Specialty)
        };

        private readonly ILaundryAdminService _laundryAdminService;
        private readonly IPriceRecommendationService _priceService;
        private readonly INotificationService _notificationService;
        private readonly NadeefDbContext _dbContext;
        private readonly IKashierService _kashierService;

        public LaundryAdminController(ILaundryAdminService laundryAdminService,
                                       IPriceRecommendationService priceService,
                                       INotificationService notificationService,
                                       NadeefDbContext dbContext,
                                       IKashierService kashierService)
        {
            _laundryAdminService = laundryAdminService;
            _priceService = priceService;
            _notificationService = notificationService;
            _dbContext = dbContext;
            _kashierService = kashierService;
        }

        // ============================================================
        // Helper - Ø¨Ù†Ø¬ÙŠØ¨ Ø§Ù„Ù€ AdminId Ù…Ù† Ø§Ù„Ù€ JWT Token ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹
        // ============================================================
        private string GetAdminId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // ============================================================
        // Ø¥Ø¹Ø¯Ø§Ø¯ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…ØºØ³Ù„Ø© Ø§Ù„Ø£Ø³Ø§Ø³ÙŠØ©
        // ============================================================

        /// <summary>
        /// POST /api/laundry-admin/setup
        /// Ø¨ÙŠØ³Ø¬Ù„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…ØºØ³Ù„Ø© Ø§Ù„Ø®Ø§ØµØ© Ø¨ÙŠÙ‡ Ù„Ø£ÙˆÙ„ Ù…Ø±Ø© Ø¨Ø¹Ø¯ Ø§Ù„ØªØ³Ø¬ÙŠÙ„
        /// </summary>
        [HttpPost("setup")]
        public async Task<IActionResult> SetupLaundry([FromBody] SetupLaundryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _laundryAdminService.SetupLaundryProfileAsync(GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry profile created successfully and pending admin approval." });
        }

        /// <summary>
        /// PUT /api/laundry-admin/profile
        /// بيعدل بيانات المغسلة الحالية الخاصة بصاحب المغسلة
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] SetupLaundryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _laundryAdminService.UpdateLaundryProfileAsync(GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry profile updated successfully." });
        }

        /// <summary>
        /// GET /api/laundry-admin/profile
        /// Ø¨ÙŠØ¬ÙŠØ¨ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…ØºØ³Ù„Ø© Ø§Ù„Ø­Ø§Ù„ÙŠØ© Ø¹Ø´Ø§Ù† ØªÙ†Ø¹Ø±Ø¶ Ù„ØµØ§Ø­Ø¨ Ø§Ù„Ù…ØºØ³Ù„Ø© (Ø§Ù„Ø§Ø³Ù…ØŒ Ø§Ù„Ù„ÙˆÙƒÙŠØ´Ù†ØŒ Ø§Ù„ØªÙ‚ÙŠÙŠÙ…ØŒ Ø§Ù„Ø­Ø§Ù„Ø©)
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _laundryAdminService.GetLaundryProfileAsync(adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage ?? "Unknown error occurred." });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/laundry-admin/services
        /// Ø¨ÙŠØ¬ÙŠØ¨ ÙƒÙ„ Ø§Ù„Ø®Ø¯Ù…Ø§Øª Ø§Ù„Ù„ÙŠ ØµØ§Ø­Ø¨ Ø§Ù„Ù…ØºØ³Ù„Ø© Ø¶Ø§ÙÙ‡Ø§ ÙÙŠ Ø§Ù„Ù†Ø¸Ø§Ù… Ù„Ù…ØºØ³Ù„ØªÙ‡
        /// </summary>
        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            try
            {
                var result = await _laundryAdminService.GetLaundryServicesAsync(adminId);

                if (!result.IsSuccess)
                {
                    // Return an empty catalog for admins who have not created a laundry yet.
                    if (string.Equals(result.ErrorMessage, "No laundry is linked to this account.", StringComparison.OrdinalIgnoreCase))
                        return Ok(Array.Empty<LaundryServiceDto>());

                    return BadRequest(new { Message = result.ErrorMessage });
                }

                return Ok(result.Value ?? new List<LaundryServiceDto>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load laundry services.", Error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/laundry-admin/dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("uid");

                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new
                    {
                        totalRevenue = 0m,
                        totalOrders = 0,
                        completedOrders = 0,
                        pendingOrders = 0
                    });
                }

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == userId);

                if (laundry == null)
                {
                    return Ok(new
                    {
                        totalRevenue = 0m,
                        totalOrders = 0,
                        completedOrders = 0,
                        pendingOrders = 0
                    });
                }

                var stats = await _dbContext.Orders
                    .Where(o => o.LaundryId == laundry.Id)
                    .GroupBy(o => o.LaundryId)
                    .Select(g => new
                    {
                        totalRevenue = g.Sum(o => o.TotalPrice),
                        totalOrders = g.Count(),
                        completedOrders = g.Count(o => o.Status == OrderStatus.Delivered),
                        pendingOrders = g.Count(o => o.Status == OrderStatus.PendingConfirmation)
                    })
                    .FirstOrDefaultAsync();

                return Ok(stats ?? new
                {
                    totalRevenue = 0m,
                    totalOrders = 0,
                    completedOrders = 0,
                    pendingOrders = 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load dashboard statistics.", Error = ex.Message });
            }
        }

        // ============================================================
        // COURIER DISPATCH OPERATIONS 
        // ============================================================

        [HttpPost("assign-courier")]
        public async Task<IActionResult> AssignCourierToOrder([FromBody] AssignCourierRequestDto request)
        {
            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            var result = await _laundryAdminService.AssignCourierToOrderExplicitlyAsync(adminId, request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);

            return Ok(new { Message = "Courier assigned and customer notified successfully." });
        }

        [HttpPost("broadcast-to-couriers")]
        public async Task<IActionResult> BroadcastToCouriers([FromBody] BroadcastOrderRequestDto request)
        {
            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            var result = await _laundryAdminService.SuggestOrderToAvailableCouriersAsync(adminId, request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);

            return Ok(new { Message = "Notified nearby couriers successfully.", AvailableCouriers = result.Value });
        }

        // ============================================================
        // Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ø·Ù„Ø¨Ø§Øª
        // ============================================================

        /// <summary>
        /// GET /api/laundry-admin/orders/incoming
        /// Ø¨ÙŠØ±Ø¬Ø¹ ÙƒÙ„ Ø§Ù„Ø·Ù„Ø¨Ø§Øª Ø§Ù„ÙˆØ§Ø±Ø¯Ø© Ø¹Ù„Ù‰ Ù…ØºØ³Ù„ØªÙ‡ Ø¨Ø³
        /// </summary>
        [HttpGet("orders/incoming")]
        public async Task<IActionResult> GetIncomingOrders()
        {
            var result = await _laundryAdminService.GetIncomingOrdersAsync(GetAdminId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/laundry-admin/orders/{id}
        /// Ø¨ÙŠØ±Ø¬Ø¹ ØªÙØ§ØµÙŠÙ„ Ø·Ù„Ø¨ Ù…Ø¹ÙŠÙ†
        /// </summary>
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _laundryAdminService.GetOrderByIdAsync(id, GetAdminId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// PUT /api/laundry-admin/orders/{id}/status
        /// Ø¨ÙŠØºÙŠØ± Ø­Ø§Ù„Ø© Ø§Ù„Ø·Ù„Ø¨ (Ù‚Ø¨ÙˆÙ„ / Ø±ÙØ¶ / InProgress / ReadyForDelivery)
        /// </summary>
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _laundryAdminService.UpdateOrderStatusAsync(id, GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Order status updated successfully" });
        }

        // ============================================================
        // Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ù…ØºØ³Ù„Ø©
        // ============================================================

        /// <summary>
        /// PUT /api/laundry-admin/availability
        /// Ø¨ÙŠÙØªØ­ Ø£Ùˆ ÙŠÙ‚ÙÙ„ Ø§Ù„Ù…ØºØ³Ù„Ø© (Open / Closed / Busy)
        /// </summary>
        [HttpPut("availability")]
        public async Task<IActionResult> UpdateAvailability([FromBody] UpdateLaundryAvailabilityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _laundryAdminService.UpdateAvailabilityAsync(GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry availability updated successfully" });
        }

        [HttpGet("availability/schedule")]
        public async Task<IActionResult> GetAvailabilitySchedule()
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
            if (laundry == null)
                return Ok(new AvailabilityScheduleDto { Days = new List<AvailabilityScheduleDayDto>() });

            var days = await _dbContext.LaundryAvailabilitySchedules
                .Where(s => s.LaundryId == laundry.Id)
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

        [HttpPut("availability/schedule")]
        public async Task<IActionResult> UpdateAvailabilitySchedule([FromBody] UpdateAvailabilityScheduleRequest request)
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "User not authenticated." });

                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid request data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                if (!IsScheduleValid(request.Days, out var validationError))
                    return BadRequest(new { Message = validationError });

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return BadRequest(new { Message = "No laundry is linked to this account." });

                var existing = await _dbContext.LaundryAvailabilitySchedules
                    .Where(s => s.LaundryId == laundry.Id)
                    .ToListAsync();

                if (existing.Count > 0)
                {
                    _dbContext.LaundryAvailabilitySchedules.RemoveRange(existing);
                    await _dbContext.SaveChangesAsync();
                }

                var updated = request.Days.Select(d => new LaundryAvailabilitySchedule
                {
                    LaundryId = laundry.Id,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to update availability schedule.", Error = ex.Message });
            }
        }

        [HttpGet("availability/capacity")]
        public async Task<IActionResult> GetCapacitySettings()
        {
            try
            {
                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
                if (laundry == null)
                    return Ok(new CapacitySettingsDto { MaxOrdersPerDay = 30, LeadTimeHours = 2 });

                var settings = await _dbContext.LaundryCapacitySettings
                    .FirstOrDefaultAsync(c => c.LaundryId == laundry.Id);

                if (settings == null)
                {
                    return Ok(new CapacitySettingsDto
                    {
                        MaxOrdersPerDay = 30,
                        LeadTimeHours = 2
                    });
                }

                return Ok(new CapacitySettingsDto
                {
                    MaxOrdersPerDay = settings.MaxOrdersPerDay,
                    LeadTimeHours = settings.LeadTimeHours
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load capacity settings.", Error = ex.Message });
            }
        }

        [HttpPut("availability/capacity")]
        public async Task<IActionResult> UpdateCapacitySettings([FromBody] CapacitySettingsDto request)
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "User not authenticated." });

                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid request data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                if (request.MaxOrdersPerDay <= 0)
                    request.MaxOrdersPerDay = 30;

                if (request.LeadTimeHours < 0)
                    request.LeadTimeHours = 0;

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return BadRequest(new { Message = "No laundry is linked to this account." });

                var settings = await _dbContext.LaundryCapacitySettings
                    .FirstOrDefaultAsync(c => c.LaundryId == laundry.Id);

                if (settings == null)
                {
                    settings = new LaundryCapacitySetting
                    {
                        LaundryId = laundry.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.LaundryCapacitySettings.AddAsync(settings);
                }

                settings.MaxOrdersPerDay = request.MaxOrdersPerDay;
                settings.LeadTimeHours = request.LeadTimeHours;
                settings.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Capacity settings updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to update capacity settings.", Error = ex.Message });
            }
        }

        [HttpGet("availability/closed-dates")]
        public async Task<IActionResult> GetClosedDates()
        {
            try
            {
                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
                if (laundry == null)
                    return Ok(new List<ClosedDateDto>());

                var dates = await _dbContext.LaundryClosedDates
                    .Where(d => d.LaundryId == laundry.Id)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load closed dates.", Error = ex.Message });
            }
        }

        [HttpPost("availability/closed-dates")]
        public async Task<IActionResult> AddClosedDate([FromBody] UpdateClosedDateRequest request)
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "User not authenticated." });

                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid request data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return BadRequest(new { Message = "No laundry is linked to this account." });

                var closedDate = request.Date.Date;
                var alreadyExists = await _dbContext.LaundryClosedDates.AnyAsync(d =>
                    d.LaundryId == laundry.Id && d.Date == closedDate);
                if (alreadyExists)
                    return BadRequest(new { Message = "This closed date already exists." });

                var entity = new LaundryClosedDate
                {
                    LaundryId = laundry.Id,
                    Date = closedDate,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.LaundryClosedDates.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Closed date added successfully", Id = entity.Id, Date = entity.Date, Reason = entity.Reason });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to add closed date.", Error = ex.Message });
            }
        }

        [HttpDelete("availability/closed-dates/{id}")]
        public async Task<IActionResult> RemoveClosedDate(int id)
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "User not authenticated." });

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return BadRequest(new { Message = "No laundry is linked to this account." });

                var entity = await _dbContext.LaundryClosedDates
                    .FirstOrDefaultAsync(d => d.Id == id && d.LaundryId == laundry.Id);

                if (entity == null)
                    return BadRequest(new { Message = "Closed date not found." });

                _dbContext.LaundryClosedDates.Remove(entity);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Closed date removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to remove closed date.", Error = ex.Message });
            }
        }

        // ============================================================
        // Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ø¯Ù„ÙŠÙØ±ÙŠ / Ø§Ù„ÙƒÙˆØ±ÙŠØ±Ø² Ø§Ù„ØªØ§Ø¨Ø¹Ø© Ù„Ù„Ù…ØºØ³Ù„Ø©
        // ============================================================

        [HttpGet("couriers")]
        public async Task<IActionResult> GetLaundryCouriers()
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
            if (laundry == null)
                return Ok(new List<LaundryCourierDto>());

            var couriers = await _dbContext.CourierProfiles
                .Where(cp => cp.LaundryId == laundry.Id)
                .Select(cp => new LaundryCourierDto
                {
                    CourierId = cp.CourierId,
                    Name = cp.Courier.Name,
                    PhoneNumber = cp.Courier.PhoneNumber ?? string.Empty,
                    IsAvailable = cp.IsAvailable,
                    CompletedOrdersCount = cp.CompletedOrdersCount,
                    CancellationCount = cp.CancellationCount,
                    LaundryId = cp.LaundryId,
                    LaundryName = cp.Laundry != null ? cp.Laundry.Name : laundry.Name
                })
                .ToListAsync();

            return Ok(couriers);
        }

        [HttpPost("couriers/assign")]
        public async Task<IActionResult> AssignCourier([FromBody] AssignCourierRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var phoneNumber = request.PhoneNumber.Trim();

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
            if (laundry == null)
                return Ok(new { message = "No laundry linked", data = (object)null });

            var courierUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Role == UserRole.Courier && u.IsActive);

            if (courierUser == null)
                return NotFound(new { Message = "Courier with this phone number was not found." });

            var profile = await _dbContext.CourierProfiles
                .FirstOrDefaultAsync(cp => cp.CourierId == courierUser.Id);

            if (profile != null && profile.LaundryId == laundry.Id)
            {
                return Ok(new
                {
                    Message = "Courier is already assigned to this laundry.",
                    CourierId = courierUser.Id,
                    LaundryId = laundry.Id
                });
            }

            if (profile != null && profile.LaundryId.HasValue && profile.LaundryId.Value != laundry.Id)
                return BadRequest(new { Message = "This courier is already assigned to another laundry." });

            if (profile == null)
            {
                profile = new CourierProfile
                {
                    CourierId = courierUser.Id,
                    LaundryId = laundry.Id,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.CourierProfiles.AddAsync(profile);
            }
            else
            {
                profile.LaundryId = laundry.Id;
                profile.IsAvailable = true;
                profile.UpdatedAt = DateTime.UtcNow;
                _dbContext.CourierProfiles.Update(profile);
            }

            await _dbContext.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                courierUser.Id,
                "Laundry Assignment",
                $"You have been assigned to {laundry.Name}. You can now receive and deliver its orders.");

            return Ok(new
            {
                Message = "Courier assigned to laundry successfully.",
                CourierId = courierUser.Id,
                LaundryId = laundry.Id
            });
        }

        [HttpDelete("couriers/{courierId}/unassign")]
        public async Task<IActionResult> UnassignCourier(string courierId)
        {
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == GetAdminId());
            if (laundry == null)
                return Ok(new { message = "No laundry linked", data = (object)null });

            var profile = await _dbContext.CourierProfiles
                .FirstOrDefaultAsync(cp => cp.CourierId == courierId && cp.LaundryId == laundry.Id);

            if (profile == null)
                return NotFound(new { Message = "Courier not found in this laundry." });

            profile.LaundryId = null;
            profile.IsAvailable = false;
            profile.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Courier unassigned successfully." });
        }

        // ============================================================
        // Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ø®Ø¯Ù…Ø§Øª
        // ============================================================

        /// <summary>
        /// POST /api/laundry-admin/services
        /// Ø¨ÙŠØ¶ÙŠÙ Ø®Ø¯Ù…Ø© Ø¬Ø¯ÙŠØ¯Ø© Ù„Ù„Ù…ØºØ³Ù„Ø©
        /// </summary>
        [HttpPost("services")]
        public async Task<IActionResult> AddService([FromBody] AddServiceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "Invalid user context." });

                var laundry = await _dbContext.Laundries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.AdminId == adminId);

                if (laundry == null)
                    return BadRequest(new { Message = "No laundry is linked to this account." });

                var normalizedName = request.ServiceName?.Trim();
                if (string.IsNullOrWhiteSpace(normalizedName))
                    return BadRequest(new { Message = "Service name is required." });

                if (request.Price <= 0)
                    return BadRequest(new { Message = "Price must be greater than 0." });

                var duplicateExists = await _dbContext.Services.AnyAsync(s =>
                    s.LaundryId == laundry.Id &&
                    s.Category == request.Category &&
                    s.ServiceName.ToLower() == normalizedName.ToLower());

                if (duplicateExists)
                    return BadRequest(new { Message = "This service already exists in your catalog." });

                var service = new Service
                {
                    ServiceName = normalizedName,
                    Category = request.Category,
                    Price = request.Price,
                    IsAvailable = request.IsAvailable,
                    LaundryId = laundry.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.Services.AddAsync(service);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Service added successfully",
                    Id = service.Id,
                    ServiceName = request.ServiceName?.Trim(),
                    Category = request.Category.ToString(),
                    Price = request.Price,
                    IsAvailable = request.IsAvailable
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to save the service.", Error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/laundry-admin/services/catalog?includeSuggestedPrice=true
        /// Default services catalog that laundry admin can enable/add with optional AI suggested prices.
        /// </summary>
        [HttpGet("services/catalog")]
        public async Task<IActionResult> GetServiceCatalog([FromQuery] bool includeSuggestedPrice = false)
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            var laundry = await _dbContext.Laundries
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.AdminId == adminId);

            var existingServices = laundry == null
                ? new List<Service>()
                : await _dbContext.Services
                    .Where(s => s.LaundryId == laundry.Id)
                    .ToListAsync();

            var result = new List<ServiceCatalogItemDto>();

            if (includeSuggestedPrice)
            {
                var tasks = ServiceCatalog.Select(async item =>
                {
                    var existing = existingServices.FirstOrDefault(s =>
                        s.Category == item.Category &&
                        string.Equals(s.ServiceName, item.Name, StringComparison.OrdinalIgnoreCase));

                    var dto = new ServiceCatalogItemDto
                    {
                        ServiceName = item.Name,
                        Category = item.Category,
                        IsAdded = existing != null,
                        ExistingServiceId = existing?.Id,
                        CurrentPrice = existing?.Price,
                        IsAvailable = existing?.IsAvailable
                    };

                    var suggestion = await _priceService.GetPriceRecommendationAsync(item.Name, item.Category);
                    if (suggestion != null)
                    {
                        dto.SuggestedMinPrice = suggestion.SuggestedMinPrice;
                        dto.SuggestedMaxPrice = suggestion.SuggestedMaxPrice;
                        dto.RecommendedPrice = suggestion.RecommendedPrice;
                        dto.SuggestionReasoning = suggestion.Reasoning;
                    }

                    return dto;
                });

                var resultsArray = await Task.WhenAll(tasks);
                result.AddRange(resultsArray);
            }
            else
            {
                foreach (var item in ServiceCatalog)
                {
                    var existing = existingServices.FirstOrDefault(s =>
                        s.Category == item.Category &&
                        string.Equals(s.ServiceName, item.Name, StringComparison.OrdinalIgnoreCase));

                    result.Add(new ServiceCatalogItemDto
                    {
                        ServiceName = item.Name,
                        Category = item.Category,
                        IsAdded = existing != null,
                        ExistingServiceId = existing?.Id,
                        CurrentPrice = existing?.Price,
                        IsAvailable = existing?.IsAvailable
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// PUT /api/laundry-admin/services/{id}
        /// Ø¨ÙŠØ¹Ø¯Ù„ Ø¨ÙŠØ§Ù†Ø§Øª Ø®Ø¯Ù…Ø© Ù…ÙˆØ¬ÙˆØ¯Ø©
        /// </summary>
        [HttpPut("services/{id}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _laundryAdminService.UpdateServiceAsync(id, GetAdminId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Service updated successfully" });
        }

        /// <summary>
        /// DELETE /api/laundry-admin/services/{id}
        /// Ø¨ÙŠÙ…Ø³Ø­ Ø®Ø¯Ù…Ø© Ù…Ù† Ø§Ù„Ù…ØºØ³Ù„Ø©
        /// </summary>
        [HttpDelete("services/{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var result = await _laundryAdminService.DeleteServiceAsync(id, GetAdminId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Service deleted successfully" });
        }
        
        // ============================================================
        // Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„ØµÙˆØ± (Cloudinary Integration)
        // ============================================================

        /// <summary>
        /// POST /api/laundry-admin/upload-image
        /// Ø¨ÙŠØ±ÙØ¹ ØµÙˆØ±Ø© Ø§Ù„Ù…ØºØ³Ù„Ø© Ø¹Ù„Ù‰ Cloudinary
        /// </summary>
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLaundryImage(IFormFile file)
        {
            // Ø¨Ù†ØªØ£ÙƒØ¯ Ø¥Ù† Ø§Ù„Ù…Ù„Ù Ù…ÙˆØ¬ÙˆØ¯
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please select an image file" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { Message = "File size cannot exceed 5MB." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { Message = "Invalid file type. Only JPG, JPEG, and PNG are allowed." });

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { Message = "Invalid file format." });

            var adminId = GetAdminId();
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized();

            var result = await _laundryAdminService.UploadLaundryImageAsync(adminId, file);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new
            {
                Message = "Image uploaded successfully",
                ImageUrl = result.Value
            });




        }
        [HttpGet("suggest-price")]
        public async Task<IActionResult> SuggestPrice([FromQuery] string serviceName, [FromQuery] ServiceCategory category)
        {
            var result = await _priceService.GetPriceRecommendationAsync(serviceName, category);
            if (result == null)
                return BadRequest("Could not get price recommendation");
            return Ok(result);
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetLaundryComplaints()
        {
            var adminId = GetAdminId();

            var data = await _dbContext.Complaints
                .Where(c => c.LaundryAdminId == adminId)
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

        [HttpPut("complaints/{id}/status")]
        public async Task<IActionResult> UpdateComplaintStatus(int id, [FromBody] UpdateComplaintStatusRequest request)
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            var complaint = await _dbContext.Complaints
                .FirstOrDefaultAsync(c => c.Id == id && c.LaundryAdminId == adminId);

            if (complaint == null)
                return NotFound(new { Message = "Complaint not found." });

            complaint.Status = request.Status;
            complaint.ResolvedAt = request.Status == ComplaintStatus.Resolved
                ? DateTime.UtcNow
                : null;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Complaint status updated successfully." });
        }

        [HttpGet("commission/summary")]
        public async Task<IActionResult> GetCommissionSummary()
        {
            var adminId = GetAdminId();
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null)
                return Ok(new { message = "No laundry linked", data = (object)null });

            const decimal commissionRate = 0.10m;

            var totalRevenue = await _dbContext.Orders
                .Where(o => o.LaundryId == laundry.Id && o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalPrice);

            var commissionPaid = await _dbContext.CommissionPayments
                .Where(p => p.LaundryId == laundry.Id && p.Status == LaundryShared.Enums.WalletEnums.CommissionPaymentStatus.Paid)
                .SumAsync(p => p.Amount);

            var commissionDue = Math.Max((totalRevenue * commissionRate) - commissionPaid, 0m);
            var status = commissionDue <= 0 ? "Paid" : "Due";

            return Ok(new CommissionSummaryDto
            {
                TotalRevenue = totalRevenue,
                CommissionRate = commissionRate * 100,
                CommissionPaid = commissionPaid,
                CommissionDue = commissionDue,
                Status = status
            });
        }

        /// <summary>
        /// GET /api/laundry-admin/payout-profile
        /// Retrieves the payout profile for the current laundry admin's laundry.
        /// </summary>
        [HttpGet("payout-profile")]
        public async Task<IActionResult> GetPayoutProfile()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            var laundry = await _dbContext.Laundries
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.AdminId == adminId);

            if (laundry == null)
                return BadRequest(new { Message = "No laundry is linked to this account." });

            var payoutProfile = await _dbContext.LaundryPayoutProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LaundryId == laundry.Id);

            if (payoutProfile == null)
            {
                return Ok(new PayoutProfileDto
                {
                    Id = 0,
                    LaundryId = laundry.Id,
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
        /// POST /api/laundry-admin/payout-profile
        /// Creates or updates the payout profile for the current laundry admin's laundry.
        /// </summary>
        [HttpPost("payout-profile")]
        public async Task<IActionResult> UpsertPayoutProfile([FromBody] UpsertPayoutProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null)
                return BadRequest(new { Message = "No laundry is linked to this account." });

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
                .FirstOrDefaultAsync(p => p.LaundryId == laundry.Id);

            if (payoutProfile == null)
            {
                payoutProfile = new LaundryPayoutProfile
                {
                    LaundryId = laundry.Id,
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

        [HttpPost("commission/kashier/initiate")]
        public async Task<IActionResult> InitiateCommissionPayment([FromBody] CommissionPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null || laundry.Id != request.LaundryId)
                return BadRequest(new { Message = "Unauthorized laundry access." });

            var result = await _kashierService.CreateCommissionPaymentAsync(adminId, request.LaundryId, request.Amount, request.Currency);
            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("payments")]
        public async Task<IActionResult> GetLaundryPayments([FromQuery] string? status = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? search = null)
        {
            var adminId = GetAdminId();
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null)
                return Ok(new { message = "No laundry linked", data = (object)null });

            var query = _dbContext.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Service)
                .Include(o => o.Customer)
                .Where(o => o.LaundryId == laundry.Id)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(o => o.CreatedAt <= to.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o => o.Id.ToString().Contains(search) ||
                                         (o.Customer != null && o.Customer.Name.Contains(search)) ||
                                         o.OrderItems.Any(i => i.Service.ServiceName.Contains(search)));
            }

            var list = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var data = list.Select(o =>
            {
                var paymentStatus = o.Payment?.PaymentStatus;
                var statusLabel = paymentStatus == PaymentStatus.Paid ? "Paid" :
                                  (o.Status == OrderStatus.Cancelled ? "Refunded" : "Pending");

                return new LaundryAdminPaymentDto
                {
                    PaymentId = o.Payment?.Id.ToString() ?? $"ORD-{o.Id}",
                    OrderId = o.Id,
                    CustomerName = o.Customer?.Name ?? "",
                    ServiceName = o.OrderItems?.FirstOrDefault()?.Service?.ServiceName ?? "",
                    Method = o.Payment?.PaymentMethod.ToString() ?? "Cash",
                    Date = o.CreatedAt,
                    Amount = o.TotalPrice,
                    Status = statusLabel
                };
            });

            if (!string.IsNullOrWhiteSpace(status))
                data = data.Where(d => d.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            return Ok(data.ToList());
        }

        [HttpGet("revenue/weekly")]
        public async Task<IActionResult> GetWeeklyRevenue()
        {
            var adminId = GetAdminId();
            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null)
                return Ok(new { message = "No laundry linked", data = (object)null });

            var start = DateTime.UtcNow.Date.AddDays(-6);

            var orders = await _dbContext.Orders
                .Where(o => o.LaundryId == laundry.Id && o.CreatedAt.Date >= start)
                .ToListAsync();

            var points = Enumerable.Range(0, 7).Select(i =>
            {
                var day = start.AddDays(i);
                var dayOrders = orders.Where(o => o.CreatedAt.Date == day && o.Status == OrderStatus.Delivered).ToList();
                return new RevenuePointDto
                {
                    Label = day.ToString("ddd"),
                    Revenue = dayOrders.Sum(o => o.TotalPrice),
                    Orders = dayOrders.Count
                };
            }).ToList();

            return Ok(points);
        }

        // ============================================================
        // Additional Dashboard & Analytics (non-conflicting routes)
        // ============================================================

        /// <summary>
        /// GET /api/laundry-admin/dashboard/summary
        /// Lightweight summary for frontend widgets
        /// </summary>
        [HttpGet("dashboard/summary-lite")]
        public async Task<IActionResult> GetDashboardSummaryLite()
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized(new { message = "Not authenticated" });

                var laundry = await _dbContext.Laundries
                    .FirstOrDefaultAsync(l => l.AdminId == adminId);

                if (laundry == null)
                {
                    return Ok(new { totalRevenue = 0, totalOrders = 0, completedOrders = 0, pendingOrders = 0, message = "Setup needed" });
                }

                var orders = await _dbContext.Orders
                    .Where(o => o.LaundryId == laundry.Id)
                    .ToListAsync();

                return Ok(new
                {
                    totalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalPrice),
                    totalOrders = orders.Count,
                    completedOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                    pendingOrders = orders.Count(o => o.Status == OrderStatus.PendingConfirmation)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/laundry-admin/orders/recent
        /// Recent orders (up to 10)
        /// </summary>
        [HttpGet("orders/recent")]
        public async Task<IActionResult> GetRecentOrders()
        {
            try
            {
                var adminId = GetAdminId();
                var laundry = await _dbContext.Laundries
                    .FirstOrDefaultAsync(l => l.AdminId == adminId);

                if (laundry == null) return Ok(new List<object>());

                var orders = await _dbContext.Orders
                    .Where(o => o.LaundryId == laundry.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new {
                        id = o.Id,
                        orderNumber = $"ORD-{o.Id.ToString().PadLeft(6, '0')}",
                        customerName = o.Customer != null ? o.Customer.Name : "Unknown",
                        serviceName = o.OrderItems.FirstOrDefault().Service != null ? o.OrderItems.FirstOrDefault().Service.ServiceName : "Service",
                        items = o.OrderItems.Count,
                        total = o.TotalPrice,
                        status = o.Status.ToString(),
                        createdAt = o.CreatedAt
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/laundry-admin/revenue/last6months
        /// Revenue grouped by month for the last 6 months
        /// </summary>
        [HttpGet("revenue/last6months")]
        public async Task<IActionResult> GetRevenueLast6Months()
        {
            try
            {
                var adminId = GetAdminId();
                var laundry = await _dbContext.Laundries
                    .FirstOrDefaultAsync(l => l.AdminId == adminId);

                if (laundry == null) return Ok(new List<object>());

                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

                var revenue = await _dbContext.Orders
                    .Where(o => o.LaundryId == laundry.Id && o.CreatedAt >= sixMonthsAgo)
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new {
                        month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        revenue = g.Sum(o => o.TotalPrice),
                        orders = g.Count()
                    })
                    .OrderBy(r => r.month)
                    .ToListAsync();

                return Ok(revenue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("revenue/monthly")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int year)
        {
            try
            {
                if (year <= 0)
                    year = DateTime.UtcNow.Year;

                var adminId = GetAdminId();
                if (string.IsNullOrWhiteSpace(adminId))
                    return Unauthorized(new { Message = "Not authenticated." });

                var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return Ok(new List<RevenuePointDto>());

                var orders = await _dbContext.Orders
                    .Where(o => o.LaundryId == laundry.Id && o.CreatedAt.Year == year && o.Status == OrderStatus.Delivered)
                    .Select(o => new { o.CreatedAt, o.TotalPrice })
                    .ToListAsync();

                var points = Enumerable.Range(1, 12).Select(m =>
                {
                    var monthOrders = orders.Where(o => o.CreatedAt.Month == m).ToList();
                    return new RevenuePointDto
                    {
                        Label = new DateTime(year, m, 1).ToString("MMM"),
                        Revenue = monthOrders.Sum(o => o.TotalPrice),
                        Orders = monthOrders.Count
                    };
                }).ToList();

                return Ok(points);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load monthly revenue.", Error = ex.Message });
            }
        }

        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var adminId = GetAdminId();
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized(new { message = "Not authenticated" });

                var laundry = await _dbContext.Laundries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.AdminId == adminId);
                if (laundry == null)
                    return Ok(new DashboardKpiDto
                    {
                        TotalOrders = 0,
                        PendingOrders = 0,
                        InProgressOrders = 0,
                        CompletedOrders = 0,
                        CancelledOrders = 0,
                        TotalRevenue = 0
                    });

                var summary = new DashboardKpiDto
                {
                    TotalOrders = await _dbContext.Orders.CountAsync(o => o.LaundryId == laundry.Id),
                    PendingOrders = await _dbContext.Orders.CountAsync(o => o.LaundryId == laundry.Id && o.Status == OrderStatus.PendingConfirmation),
                    InProgressOrders = await _dbContext.Orders.CountAsync(o =>
                        o.LaundryId == laundry.Id &&
                        (o.Status == OrderStatus.Accepted ||
                         o.Status == OrderStatus.PickedUp ||
                         o.Status == OrderStatus.Washing ||
                         o.Status == OrderStatus.ReadyForPickup)),
                    CompletedOrders = await _dbContext.Orders.CountAsync(o => o.LaundryId == laundry.Id && o.Status == OrderStatus.Delivered),
                    CancelledOrders = await _dbContext.Orders.CountAsync(o => o.LaundryId == laundry.Id && o.Status == OrderStatus.Cancelled),
                    TotalRevenue = await _dbContext.Orders
                        .Where(o => o.LaundryId == laundry.Id && o.Status == OrderStatus.Delivered)
                        .Select(o => (decimal?)o.TotalPrice)
                        .SumAsync() ?? 0
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to load dashboard summary.", Error = ex.Message });
            }
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

        /// <summary>
        /// GET /api/laundry-admin/payouts
        /// </summary>
        [HttpGet("payouts")]
        public async Task<IActionResult> GetLaundryPayouts()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "Invalid user context." });

            var laundry = await _dbContext.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminId);
            if (laundry == null)
                return BadRequest(new { Message = "No laundry is linked to this account." });

            var payouts = await _dbContext.LaundryPayouts
                .Where(p => p.LaundryId == laundry.Id)
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

