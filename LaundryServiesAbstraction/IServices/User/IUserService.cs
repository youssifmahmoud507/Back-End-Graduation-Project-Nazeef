using LaundryShared.Common;
using LaundryShared.DTO.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.User
{
    public interface IUserService
    {
        // GET /api/user/profile
        Task<Result<UserProfileDto>> GetProfileAsync(string userId);

        // PUT /api/user/profile
        Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request);

        // PUT /api/user/change-password
        Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request);

        // DELETE /api/user/account
        Task<Result<bool>> DeleteAccountAsync(string userId, DeleteAccountRequest request);

        // GET /api/user/stats
        Task<Result<UserStatsDto>> GetStatsAsync(string userId);

        // GET /api/user/addresses
        Task<Result<List<AddressDto>>> GetAddressesAsync(string userId);

        // POST /api/user/addresses
        Task<Result<AddressDto>> AddAddressAsync(string userId, AddAddressRequest request);

        // PUT /api/user/addresses/:id
        Task<Result<AddressDto>> UpdateAddressAsync(string userId, int addressId, AddAddressRequest request);

        // DELETE /api/user/addresses/:id
        Task<Result<bool>> DeleteAddressAsync(string userId, int addressId);

        // PUT /api/user/addresses/:id/set-default
        Task<Result<bool>> SetDefaultAddressAsync(string userId, int addressId);

        // GET /api/user/payment-methods
        Task<Result<List<UserPaymentMethodDto>>> GetPaymentMethodsAsync(string userId);

        // POST /api/user/payment-methods
        Task<Result<UserPaymentMethodDto>> AddPaymentMethodAsync(string userId, AddPaymentMethodRequest request);

        // DELETE /api/user/payment-methods/:id
        Task<Result<bool>> DeletePaymentMethodAsync(string userId, int paymentMethodId);

        // PUT /api/user/payment-methods/:id/set-default
        Task<Result<bool>> SetDefaultPaymentMethodAsync(string userId, int paymentMethodId);

        // GET /api/user/favorites
        Task<Result<List<FavoriteLaundryDto>>> GetFavoritesAsync(string userId);

        // POST /api/user/favorites
        Task<Result<bool>> AddFavoriteAsync(string userId, int laundryId);

        // DELETE /api/user/favorites/:laundryId
        Task<Result<bool>> RemoveFavoriteAsync(string userId, int laundryId);

        // GET /api/user/points
        Task<Result<PointsDto>> GetPointsAsync(string userId);

        // POST /api/user/points/redeem
        Task<Result<bool>> RedeemPointsAsync(string userId, RedeemPointsRequest request);

        // GET /api/user/settings
        Task<Result<UserSettingsDto>> GetSettingsAsync(string userId);

        // PUT /api/user/settings
        Task<Result<UserSettingsDto>> UpdateSettingsAsync(string userId, UpdateUserSettingsRequest request);

        // Upload user avatar image
        Task<Result<string>> UploadAvatarAsync(string userId, Microsoft.AspNetCore.Http.IFormFile file);
    }
}
