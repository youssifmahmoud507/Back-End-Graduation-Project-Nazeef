using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.DTO.PaymentKashier;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IKashierService _kashierService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IKashierService kashierService, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _kashierService = kashierService;
            _configuration = configuration;
            _logger = logger;
        }

        private static bool IsSuccessfulStatus(string? status) =>
            string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

        private string ResolveMerchantOrderId(string? fallbackMerchantOrderId)
        {
            if (!string.IsNullOrWhiteSpace(fallbackMerchantOrderId))
                return fallbackMerchantOrderId;

            var candidateKeys = new[]
            {
                "merchantOrderId",
                "merchant_order_id",
                "order",
                "orderId",
                "merchantOrderReference"
            };

            foreach (var key in candidateKeys)
            {
                var value = Request.Query[key].ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        // GET: /payment/callback
        // Kashier redirects the client browser here after payment completion
        [HttpGet("/payment/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> KashierCallback([FromQuery] string? status, [FromQuery] string? merchantOrderId)
        {
            var frontendBaseUrl = _configuration["Kashier:FrontendBaseUrl"];
            if (string.IsNullOrEmpty(frontendBaseUrl) || frontendBaseUrl.Contains("localhost"))
            {
                if (Request.Host.Host.Contains("azurecontainerapps.io", StringComparison.OrdinalIgnoreCase) ||
                    Request.Host.Host.Contains("ndeefapp.runasp.net", StringComparison.OrdinalIgnoreCase))
                {
                    frontendBaseUrl = "https://www.nazeef.tech";
                }
            }

            frontendBaseUrl = frontendBaseUrl?.TrimEnd('/') ?? "https://www.nazeef.tech";
            string redirectUrl = $"{frontendBaseUrl}/";
            var resolvedMerchantOrderId = ResolveMerchantOrderId(merchantOrderId);

            // Resolve final status checking both query params (status and paymentStatus)
            var finalStatus = Request.Query["status"].ToString();
            if (string.IsNullOrEmpty(finalStatus))
            {
                finalStatus = Request.Query["paymentStatus"].ToString();
            }
            if (string.IsNullOrEmpty(finalStatus))
            {
                finalStatus = status;
            }

            // Log incoming parameters for debugging
            _logger.LogInformation("[Kashier Callback] Received callback. MerchantOrderId: {MerchantOrderId}, Status: {Status}, QueryString: {QueryString}", 
                resolvedMerchantOrderId, finalStatus, Request.QueryString.Value);

            if (!string.IsNullOrEmpty(resolvedMerchantOrderId) && IsSuccessfulStatus(finalStatus))
            {
                decimal amount = 0;
                
                // Parse amount safely using InvariantCulture to avoid server culture mismatch (comma vs dot)
                var amountStr = Request.Query["amount"].ToString();
                if (string.IsNullOrEmpty(amountStr))
                {
                    // Fallback to Kashier's native parameter if amount is not found
                    amountStr = Request.Query["paymentAmount"].ToString();
                }

                if (!string.IsNullOrEmpty(amountStr) && 
                    decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedAmount))
                {
                    amount = parsedAmount;
                }

                _logger.LogInformation("[Kashier Callback] Parsed callback amount: {Amount}", amount);

                await _kashierService.ProcessCallbackPaymentAsync(resolvedMerchantOrderId, amount, finalStatus);
            }

            if (!string.IsNullOrEmpty(resolvedMerchantOrderId))
            {
                if (resolvedMerchantOrderId.StartsWith("order-"))
                {
                    var parts = resolvedMerchantOrderId.Split('-');
                    if (parts.Length >= 2)
                    {
                        var orderId = parts[1];
                        if (IsSuccessfulStatus(status) || IsSuccessfulStatus(finalStatus))
                        {
                            redirectUrl = $"{frontendBaseUrl}/track-order/{orderId}?notice=paid";
                        }
                        else
                        {
                            redirectUrl = $"{frontendBaseUrl}/payment?orderId={orderId}&status=failed";
                        }
                    }
                }
                else if (resolvedMerchantOrderId.StartsWith("commission-"))
                {
                    var nextStatus = (IsSuccessfulStatus(status) || IsSuccessfulStatus(finalStatus)) ? "success" : "failed";
                    redirectUrl = $"{frontendBaseUrl}/laundry-admin/payments?payment={Uri.EscapeDataString(nextStatus)}&merchantOrderId={Uri.EscapeDataString(resolvedMerchantOrderId)}";
                }
                else if (resolvedMerchantOrderId.StartsWith("wallet-charge-"))
                {
                    var nextStatus = (IsSuccessfulStatus(status) || IsSuccessfulStatus(finalStatus)) ? "success" : "failed";
                    redirectUrl = $"{frontendBaseUrl}/wallet?status={nextStatus}&merchantOrderId={Uri.EscapeDataString(resolvedMerchantOrderId)}";
                }
            }

            return Redirect(redirectUrl);
        }

        // POST: api/payment/webhook
        // هذا هو الـ Endpoint الوحيد المسجّل عند Kashier كـ serverWebhook
        // كاشير بيبعت الـ Webhook هنا لما الدفع ينجح أو يفشل
        [HttpPost("webhook")]
        public async Task<IActionResult> KashierWebhook([FromHeader(Name = "x-kashier-signature")] string? signature, [FromBody] KashierWebhookDto webhook)
        {
            var result = await _kashierService.HandleWebhookAsync(signature ?? string.Empty, webhook);

            // دائماً نرجع 200 OK لكاشير حتى لو العملية غير ناجحة أو بحالة مختلفة
            // لأن إرجاع 400/500 بيخلي كاشير يُعيد إرسال الـ Webhook مراراً وتكراراً (Retry Loop)
            // التسجيل الكامل للنتيجة موجود في الـ Logs داخل HandleWebhookAsync
            return Ok(new { received = true, success = result.IsSuccess });
        }
    }
}
