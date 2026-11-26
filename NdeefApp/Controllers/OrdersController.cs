using LaundryServiesAbstraction.IServices.Order;
using LaundryShared.DTO.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private string GetCustomerId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // POST /api/orders/calculate-price
        [HttpPost("calculate-price")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CalculatePrice([FromBody] CalculatePriceRequest request)
        {
            var result = await _orderService.CalculatePriceAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // POST /api/orders
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var result = await _orderService.PlaceOrderAsync(GetCustomerId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/orders
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerOrders([FromQuery] OrderParams orderParams)
        {
            var result = await _orderService.GetCustomerOrdersAsync(GetCustomerId(), orderParams);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/orders/active
        [HttpGet("active")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetActiveOrders()
        {
            var result = await _orderService.GetActiveOrdersAsync(GetCustomerId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/orders/history
        [HttpGet("history")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrderHistory([FromQuery] OrderParams orderParams)
        {
            var result = await _orderService.GetOrderHistoryAsync(GetCustomerId(), orderParams);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/orders/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _orderService.GetOrderByIdAsync(GetCustomerId(), id);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/orders/{id}/track
        [HttpGet("{id}/track")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> TrackOrder(int id)
        {
            var result = await _orderService.TrackOrderAsync(GetCustomerId(), id);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/orders/{id}/cancel
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
        {
            var result = await _orderService.CancelOrderAsync(GetCustomerId(), id, request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Order cancelled successfully." });
        }

        // GET /api/orders/{id}/payment
        [HttpGet("{id}/payment")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrderPayment(int id)
        {
            var result = await _orderService.GetOrderPaymentAsync(GetCustomerId(), id);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }
    }
}
