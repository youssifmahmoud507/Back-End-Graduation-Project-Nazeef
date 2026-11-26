using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.DTO.Order;
using LaundryShared.DTO.PaymentKashier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IKashierService _kashierService;

        public PaymentsController(IPaymentService paymentService, IKashierService kashierService)
        {
            _paymentService = paymentService;
            _kashierService = kashierService;
        }

        private string GetCustomerId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // POST /api/payments/process
        [HttpPost("process")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            var result = await _paymentService.ProcessPaymentAsync(GetCustomerId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // تحذير: الـ Endpoint الصحيح المسجّل عند Kashier هو /api/payment/webhook (في PaymentController.cs)
        // هذا الـ Endpoint موجود فقط كـ fallback ولا يجب الاعتماد عليه لمراقبة عمليات الدفع
        [HttpPost("kashier/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> KashierWebhook([FromHeader(Name = "x-kashier-signature")] string? signature, [FromBody] KashierWebhookDto webhook)
        {
            var result = await _kashierService.HandleWebhookAsync(signature ?? string.Empty, webhook);

            // دائماً نرجع 200 OK لكاشير حتى لو العملية غير ناجحة
            // عشان نمنع Kashier من إعادة إرسال الـ Webhook مراراً (Retry Loop)
            return Ok(new { received = true, success = result.IsSuccess });
        }

        // POST /api/payments/confirm
        [HttpPost("{paymentId}/confirm")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ConfirmPayment(int paymentId)
        {
            var result = await _paymentService.ConfirmPaymentAsync(GetCustomerId(), paymentId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/payments/methods
        [HttpGet("methods")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var result = await _paymentService.GetPaymentMethodsAsync();
            return Ok(result.Value);
        }

        // GET /api/payments/history
        [HttpGet("history")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var result = await _paymentService.GetPaymentHistoryAsync(GetCustomerId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }




    }
}
