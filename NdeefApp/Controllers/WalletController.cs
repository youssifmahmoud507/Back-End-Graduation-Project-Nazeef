using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.DTO.PaymentKashier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LaundryPerstince.Data.Context;
using LaundryDomain.Models.Wallet;

namespace LaundryApi.Controllers
{

    /// <summary>
    /// البوابة بتاعة المحفظة والدفع
    /// </summary>
    [Route("api/wallet")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IKashierService _kashierService;
        private readonly NadeefDbContext _dbContext;

        public WalletController(IKashierService kashierService, NadeefDbContext dbContext)
        {
            _kashierService = kashierService;
            _dbContext = dbContext;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;


        // ============================================================
        // محفظة العميل
        // ============================================================

        /// <summary>
        /// GET /api/wallet/info
        /// الحصول على تفاصيل المحفظة والرصيد والعمليات السابقة
        /// </summary>
        [HttpGet("info")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetWalletInfo()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var customerWallet = await _dbContext.CustomerWallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.CustomerId == userId);

            if (customerWallet == null)
            {
                customerWallet = new CustomerWallet
                {
                    CustomerId = userId,
                    Balance = 0,
                    TotalCharged = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.CustomerWallets.Add(customerWallet);
                await _dbContext.SaveChangesAsync();
            }

            var dto = new CustomerWalletDto
            {
                Balance = customerWallet.Balance,
                TotalCharged = customerWallet.TotalCharged,
                IsActive = customerWallet.IsActive,
                Transactions = customerWallet.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new WalletTransactionDto
                    {
                        Id = t.Id,
                        OrderId = t.OrderId,
                        Amount = t.Amount,
                        PaymentReference = t.PaymobTransactionId,
                        Type = t.Type.ToString(),
                        Source = t.Source.ToString(),
                        Status = t.Status.ToString(),
                        CreatedAt = t.CreatedAt
                    }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost("charge")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ChargeWallet([FromBody] ChargeWalletRequest request)
        {
            return BadRequest(new { Message = "Wallet charging is disabled. Wallet can only be funded via order refunds." });
        }

        /// <summary>
        /// POST /api/wallet/pay/mobile-wallet/{orderId}
        /// العميل بيدفع الأوردر بـ Mobile Wallet
        /// </summary>
        [HttpPost("pay/mobile-wallet/{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PayWithMobileWallet(int orderId, [FromBody] ChargeWalletRequest request)
        {
            return BadRequest(new { Message = "Mobile wallet payment is disabled. Please pay with cash or credit card." });
        }

        /// <summary>
        /// POST /api/wallet/pay/cash/{orderId}
        /// العميل بيختار الدفع كاش عند الاستلام
        /// </summary>
        [HttpPost("pay/cash/{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PayWithCash(int orderId, [FromBody] ChargeWalletRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _kashierService.CreateCashPaymentAsync(
                GetUserId(), orderId, request.Amount);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Cash payment registered successfully" });
        }

        // ============================================================
        // Kashier Webhook — تحذير: هذا الـ Endpoint لا تسجّله في كاشير
        // الـ Endpoint الصحيح الوحيد المسجّل عند كاشير هو:
        //   POST /api/payment/webhook  (في PaymentController.cs)
        // هذا الـ Endpoint هنا موجود فقط كـ fallback للطلبات القديمة
        // ولا يجب الاعتماد عليه أو مراقبته للتحقق من نجاح عمليات الشحن
        // ============================================================

        /// <summary>
        /// POST /api/wallet/webhook  [DEPRECATED — Fallback only]
        /// الـ Endpoint الفعلي المسجّل عند Kashier هو /api/payment/webhook
        /// هذا الـ Endpoint مش هيستقبل أي webhook من كاشير في الإعداد الحالي
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook([FromHeader(Name = "x-kashier-signature")] string? signature, [FromBody] KashierWebhookDto webhook)
        {
            // Fallback: نفس المنطق الخاص بـ /api/payment/webhook
            // لكن تأكد أن كاشير مسجّل فقط على /api/payment/webhook
            var result = await _kashierService.HandleWebhookAsync(signature ?? string.Empty, webhook);

            // دائماً نرجع 200 OK لكاشير حتى لو العملية غير ناجحة
            // عشان نمنع Kashier من إعادة إرسال الـ Webhook مراراً (Retry Loop)
            return Ok(new { received = true, success = result.IsSuccess });
        }
    }
}
