using LaundryServiesAbstraction.IServices.User;
using LaundryShared.DTO.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        public sealed class AvatarUploadRequest
        {
            public IFormFile File { get; set; } = default!;
        }

        // POST /api/user/avatar
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar([FromForm] AvatarUploadRequest request)
        {
            var result = await _userService.UploadAvatarAsync(GetUserId(), request.File);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { AvatarUrl = result.Value });
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // ==================== Profile ====================

        // GET /api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _userService.GetProfileAsync(GetUserId());

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.UpdateProfileAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/user/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.ChangePasswordAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Password changed successfully." });
        }

        // DELETE /api/user/account
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.DeleteAccountAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Account deleted successfully." });
        }

        // ==================== Stats ====================

        // GET /api/user/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _userService.GetStatsAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ==================== Addresses ====================

        // GET /api/user/addresses
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var result = await _userService.GetAddressesAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // POST /api/user/addresses
        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.AddAddressAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/user/addresses/:id
        [HttpPut("addresses/{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddAddressRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.UpdateAddressAsync(GetUserId(), id, request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // DELETE /api/user/addresses/:id
        [HttpDelete("addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var result = await _userService.DeleteAddressAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Address deleted successfully." });
        }

        // PUT /api/user/addresses/:id/set-default
        [HttpPut("addresses/{id}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var result = await _userService.SetDefaultAddressAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Default address updated successfully." });
        }

        // ==================== Payment Methods ====================

        // GET /api/user/payment-methods
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var result = await _userService.GetPaymentMethodsAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // POST /api/user/payment-methods
        [HttpPost("payment-methods")]
        public async Task<IActionResult> AddPaymentMethod([FromBody] AddPaymentMethodRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.AddPaymentMethodAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // DELETE /api/user/payment-methods/:id
        [HttpDelete("payment-methods/{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var result = await _userService.DeletePaymentMethodAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Payment method deleted successfully." });
        }

        // PUT /api/user/payment-methods/:id/set-default
        [HttpPut("payment-methods/{id}/set-default")]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var result = await _userService.SetDefaultPaymentMethodAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Default payment method updated successfully." });
        }

        // ==================== Favorites ====================

        // GET /api/user/favorites
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var result = await _userService.GetFavoritesAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // POST /api/user/favorites
        [HttpPost("favorites")]
        public async Task<IActionResult> AddFavorite([FromBody] int laundryId)
        {
            var result = await _userService.AddFavoriteAsync(GetUserId(), laundryId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry added to favorites." });
        }

        // DELETE /api/user/favorites/:laundryId
        [HttpDelete("favorites/{laundryId}")]
        public async Task<IActionResult> RemoveFavorite(int laundryId)
        {
            var result = await _userService.RemoveFavoriteAsync(GetUserId(), laundryId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Laundry removed from favorites." });
        }

        // ==================== Points ====================

        // GET /api/user/points
        [HttpGet("points")]
        public async Task<IActionResult> GetPoints()
        {
            var result = await _userService.GetPointsAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // POST /api/user/points/redeem
        [HttpPost("points/redeem")]
        public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.RedeemPointsAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Points redeemed successfully." });
        }

        // ==================== Settings ====================

        // GET /api/user/settings
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var result = await _userService.GetSettingsAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/user/settings
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.UpdateSettingsAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }
    }
}
