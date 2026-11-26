using LaundryDomain.IRepositories;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.OrderSpecs;
using LaundryDomain.Specifications.UserSpecs;
using LaundryServiesAbstraction.IServices.User;
using LaundryShared.Common;
using LaundryShared.DTO.User;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.UserModule
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LaundryServies.Common.IFileService _fileService;

        public UserService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, LaundryServies.Common.IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _fileService = fileService;
        }

        // GET /api/user/profile
        public async Task<Result<UserProfileDto>> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Result<UserProfileDto>.Failure("User not found.");

            return Result<UserProfileDto>.Success(new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            });
        }

        public async Task<Result<string>> UploadAvatarAsync(string userId, Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Result<string>.Failure("Please select an image file.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<string>.Failure("User not found.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return Result<string>.Failure("Only image files are allowed (jpg, jpeg, png, webp)");

            if (file.Length > 5 * 1024 * 1024)
                return Result<string>.Failure("Image size must not exceed 5MB");

            // Upload
            var imageUrl = await _fileService.UploadImageAsync(file, "avatars");

            // Delete old one if exists
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                try
                {
                    var oldPublicId = Path.ChangeExtension(new Uri(user.AvatarUrl).AbsolutePath.TrimStart('/'), null);
                    if (!string.IsNullOrEmpty(oldPublicId))
                        await _fileService.DeleteImageAsync(oldPublicId);
                }
                catch { /* ignore parsing errors */ }
            }

            user.AvatarUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result<string>.Failure("Failed to save avatar");

            return Result<string>.Success(imageUrl);
        }

        // PUT /api/user/profile
        public async Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Result<UserProfileDto>.Failure("User not found.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.UserName = request.Email;
            user.PhoneNumber = request.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserProfileDto>.Failure($"Update failed: {errors}");
            }

            return Result<UserProfileDto>.Success(new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            });
        }

        // PUT /api/user/change-password
        public async Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Result<bool>.Failure("User not found.");

            // Verify old password manually before proceeding
            var isOldPasswordCorrect = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isOldPasswordCorrect)
                return Result<bool>.Failure("Current password is not correct.");

            // Directly remove the old password and add the new one, bypassing caching issues
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return Result<bool>.Failure("Failed to process password update.");

            var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result<bool>.Failure($"Password change failed: {errors}");
            }

            // Update Security Stamp to invalidate old sessions/tokens across all devices
            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.UpdateAsync(user);

            return Result<bool>.Success(true);
        }

        // DELETE /api/user/account
        public async Task<Result<bool>> DeleteAccountAsync(string userId, DeleteAccountRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Result<bool>.Failure("User not found.");

            var passwordCheck = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordCheck)
                return Result<bool>.Failure("Invalid password.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return Result<bool>.Failure("Account deletion failed.");

            return Result<bool>.Success(true);
        }

        // GET /api/user/stats
        public async Task<Result<UserStatsDto>> GetStatsAsync(string userId)
        {
            var activeOrdersSpec = new ActiveOrdersByCustomerSpec(userId);
            var historySpec = new OrderHistorySpec(userId, new LaundryShared.DTO.Order.OrderParams());
            var addressesSpec = new UserAddressesByUserSpec(userId);
            var favoritesSpec = new UserFavoritesByUserSpec(userId);

            var activeOrders = await _unitOfWork.Repository<Order, int>()
                                                .GetAllWithSpecAsync(activeOrdersSpec);

            var historyOrders = await _unitOfWork.Repository<Order, int>()
                                                 .GetAllWithSpecAsync(historySpec);

            var addresses = await _unitOfWork.Repository<UserAddress, int>()
                                             .GetAllWithSpecAsync(addressesSpec);

            var favorites = await _unitOfWork.Repository<UserFavorite, int>()
                                             .GetAllWithSpecAsync(favoritesSpec);

            var pointsSpec = new UserPointsByUserSpec(userId);
            var userPoints = await _unitOfWork.Repository<UserPoints, int>()
                                              .GetEntityWithSpecAsync(pointsSpec);

            var totalSpent = historyOrders
                .Where(o => o.Status == OrderStatus.Delivered)
                .Sum(o => o.TotalPrice);

            return Result<UserStatsDto>.Success(new UserStatsDto
            {
                ActiveOrders = activeOrders.Count,
                CompletedOrders = historyOrders.Count(o => o.Status == OrderStatus.Delivered),
                TotalSpent = totalSpent,
                Points = userPoints?.TotalPoints ?? 0,
                SavedAddresses = addresses.Count,
                FavoriteLaundries = favorites.Count
            });
        }

        // GET /api/user/addresses
        public async Task<Result<List<AddressDto>>> GetAddressesAsync(string userId)
        {
            var spec = new UserAddressesByUserSpec(userId);
            var addresses = await _unitOfWork.Repository<UserAddress, int>()
                                             .GetAllWithSpecAsync(spec);

            var data = addresses.Select(a => MapToAddressDto(a)).ToList();

            return Result<List<AddressDto>>.Success(data);
        }

        // POST /api/user/addresses
        public async Task<Result<AddressDto>> AddAddressAsync(string userId, AddAddressRequest request)
        {
            // لو IsDefault نشيل الـ Default القديمة
            if (request.IsDefault)
                await ResetDefaultAddressAsync(userId);

            var address = new UserAddress
            {
                UserId = userId,
                Type = request.Type,
                Label = request.Label,
                Street = request.Street,
                Apt = request.Apt,
                City = request.City,
                Area = request.Area,
                Instructions = request.Instructions,
                IsDefault = request.IsDefault
            };

            await _unitOfWork.Repository<UserAddress, int>().AddAsync(address);
            await _unitOfWork.CompleteAsync();

            return Result<AddressDto>.Success(MapToAddressDto(address));
        }

        // PUT /api/user/addresses/:id
        public async Task<Result<AddressDto>> UpdateAddressAsync(string userId, int addressId, AddAddressRequest request)
        {
            var spec = new UserAddressByIdSpec(addressId, userId);
            var address = await _unitOfWork.Repository<UserAddress, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (address == null)
                return Result<AddressDto>.Failure("Address not found.");

            if (request.IsDefault)
                await ResetDefaultAddressAsync(userId);

            address.Type = request.Type;
            address.Label = request.Label;
            address.Street = request.Street;
            address.Apt = request.Apt;
            address.City = request.City;
            address.Area = request.Area;
            address.Instructions = request.Instructions;
            address.IsDefault = request.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UserAddress, int>().Update(address);
            await _unitOfWork.CompleteAsync();

            return Result<AddressDto>.Success(MapToAddressDto(address));
        }

        // DELETE /api/user/addresses/:id
        public async Task<Result<bool>> DeleteAddressAsync(string userId, int addressId)
        {
            var spec = new UserAddressByIdSpec(addressId, userId);
            var address = await _unitOfWork.Repository<UserAddress, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (address == null)
                return Result<bool>.Failure("Address not found.");

            _unitOfWork.Repository<UserAddress, int>().Delete(address);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // PUT /api/user/addresses/:id/set-default
        public async Task<Result<bool>> SetDefaultAddressAsync(string userId, int addressId)
        {
            var spec = new UserAddressByIdSpec(addressId, userId);
            var address = await _unitOfWork.Repository<UserAddress, int>()
                                           .GetEntityWithSpecAsync(spec);

            if (address == null)
                return Result<bool>.Failure("Address not found.");

            await ResetDefaultAddressAsync(userId);

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UserAddress, int>().Update(address);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // GET /api/user/payment-methods
        public async Task<Result<List<UserPaymentMethodDto>>> GetPaymentMethodsAsync(string userId)
        {
            var spec = new UserPaymentMethodsByUserSpec(userId);
            var methods = await _unitOfWork.Repository<UserPaymentMethod, int>()
                                           .GetAllWithSpecAsync(spec);

            var data = methods.Select(m => MapToPaymentMethodDto(m)).ToList();

            return Result<List<UserPaymentMethodDto>>.Success(data);
        }

        // POST /api/user/payment-methods
        public async Task<Result<UserPaymentMethodDto>> AddPaymentMethodAsync(string userId, AddPaymentMethodRequest request)
        {
            if (request.IsDefault)
                await ResetDefaultPaymentMethodAsync(userId);

            var method = new UserPaymentMethod
            {
                UserId = userId,
                Type = request.Type,
                CardLast4 = request.CardNumber.Substring(request.CardNumber.Length - 4),
                CardBrand = DetectCardBrand(request.CardNumber),
                CardholderName = request.CardholderName,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                IsDefault = request.IsDefault
            };

            await _unitOfWork.Repository<UserPaymentMethod, int>().AddAsync(method);
            await _unitOfWork.CompleteAsync();

            return Result<UserPaymentMethodDto>.Success(MapToPaymentMethodDto(method));
        }

        // DELETE /api/user/payment-methods/:id
        public async Task<Result<bool>> DeletePaymentMethodAsync(string userId, int paymentMethodId)
        {
            var spec = new UserPaymentMethodsByUserSpec(userId);
            var methods = await _unitOfWork.Repository<UserPaymentMethod, int>()
                                           .GetAllWithSpecAsync(spec);

            var method = methods.FirstOrDefault(m => m.Id == paymentMethodId);

            if (method == null)
                return Result<bool>.Failure("Payment method not found.");

            _unitOfWork.Repository<UserPaymentMethod, int>().Delete(method);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // PUT /api/user/payment-methods/:id/set-default
        public async Task<Result<bool>> SetDefaultPaymentMethodAsync(string userId, int paymentMethodId)
        {
            var spec = new UserPaymentMethodsByUserSpec(userId);
            var methods = await _unitOfWork.Repository<UserPaymentMethod, int>()
                                           .GetAllWithSpecAsync(spec);

            var method = methods.FirstOrDefault(m => m.Id == paymentMethodId);

            if (method == null)
                return Result<bool>.Failure("Payment method not found.");

            await ResetDefaultPaymentMethodAsync(userId);

            method.IsDefault = true;
            method.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UserPaymentMethod, int>().Update(method);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // GET /api/user/favorites
        public async Task<Result<List<FavoriteLaundryDto>>> GetFavoritesAsync(string userId)
        {
            var spec = new UserFavoritesByUserSpec(userId);
            var favorites = await _unitOfWork.Repository<UserFavorite, int>()
                                             .GetAllWithSpecAsync(spec);

            var data = favorites.Select(f => new FavoriteLaundryDto
            {
                Id = f.Id,
                LaundryId = f.LaundryId,
                LaundryName = f.Laundry?.Name,
                LaundryAddress = f.Laundry?.Address,
                AverageRating = f.Laundry?.AverageRating ?? 0,
                AddedAt = f.CreatedAt
            }).ToList();

            return Result<List<FavoriteLaundryDto>>.Success(data);
        }

        // POST /api/user/favorites
        public async Task<Result<bool>> AddFavoriteAsync(string userId, int laundryId)
        {
            var spec = new UserFavoriteByLaundrySpec(userId, laundryId);
            var existing = await _unitOfWork.Repository<UserFavorite, int>()
                                            .GetEntityWithSpecAsync(spec);

            if (existing != null)
                return Result<bool>.Failure("Laundry already in favorites.");

            var favorite = new UserFavorite
            {
                UserId = userId,
                LaundryId = laundryId
            };

            await _unitOfWork.Repository<UserFavorite, int>().AddAsync(favorite);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // DELETE /api/user/favorites/:laundryId
        public async Task<Result<bool>> RemoveFavoriteAsync(string userId, int laundryId)
        {
            var spec = new UserFavoriteByLaundrySpec(userId, laundryId);
            var favorite = await _unitOfWork.Repository<UserFavorite, int>()
                                            .GetEntityWithSpecAsync(spec);

            if (favorite == null)
                return Result<bool>.Failure("Laundry not in favorites.");

            _unitOfWork.Repository<UserFavorite, int>().Delete(favorite);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // GET /api/user/points
        public async Task<Result<PointsDto>> GetPointsAsync(string userId)
        {
            var spec = new UserPointsByUserSpec(userId);
            var userPoints = await _unitOfWork.Repository<UserPoints, int>()
                                              .GetEntityWithSpecAsync(spec);

            if (userPoints == null)
                return Result<PointsDto>.Success(new PointsDto
                {
                    TotalPoints = 0,
                    History = new List<PointTransactionDto>()
                });

            var data = new PointsDto
            {
                TotalPoints = userPoints.TotalPoints,
                History = userPoints.Transactions.Select(t => new PointTransactionDto
                {
                    Id = t.Id,
                    Points = t.Points,
                    Type = t.Type,
                    Description = t.Description,
                    OrderId = t.OrderId,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };

            return Result<PointsDto>.Success(data);
        }

        // POST /api/user/points/redeem
        public async Task<Result<bool>> RedeemPointsAsync(string userId, RedeemPointsRequest request)
        {
            var spec = new UserPointsByUserSpec(userId);
            var userPoints = await _unitOfWork.Repository<UserPoints, int>()
                                              .GetEntityWithSpecAsync(spec);

            if (userPoints == null)
                return Result<bool>.Failure("No points found.");

            if (userPoints.TotalPoints < request.Points)
                return Result<bool>.Failure("Insufficient points.");

            userPoints.TotalPoints -= request.Points;

            var transaction = new PointTransaction
            {
                Points = request.Points,
                Type = LaundryShared.Enums.CustomerEnums.PointTransactionType.Redeemed,
                Description = $"Redeemed {request.Points} points for reward {request.RewardId}",
                UserPointsId = userPoints.Id
            };

            userPoints.Transactions.Add(transaction);
            userPoints.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UserPoints, int>().Update(userPoints);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // GET /api/user/settings
        public async Task<Result<UserSettingsDto>> GetSettingsAsync(string userId)
        {
            var spec = new UserSettingsByUserSpec(userId);
            var settings = await _unitOfWork.Repository<UserSettings, int>()
                                            .GetEntityWithSpecAsync(spec);

            if (settings == null)
                return Result<UserSettingsDto>.Success(new UserSettingsDto
                {
                    Language = LaundryShared.Enums.CustomerEnums.Language.Arabic,
                    Currency = LaundryShared.Enums.CustomerEnums.Currency.EGP,
                    PushNotifications = true,
                    EmailNotifications = true,
                    SmsNotifications = true,
                    WhatsappNotifications = true,
                    ShowProfile = true,
                    ShareData = false
                });

            return Result<UserSettingsDto>.Success(new UserSettingsDto
            {
                Language = settings.Language,
                Currency = settings.Currency,
                PushNotifications = settings.PushNotifications,
                EmailNotifications = settings.EmailNotifications,
                SmsNotifications = settings.SmsNotifications,
                WhatsappNotifications = settings.WhatsappNotifications,
                ShowProfile = settings.ShowProfile,
                ShareData = settings.ShareData
            });
        }

        // PUT /api/user/settings
        public async Task<Result<UserSettingsDto>> UpdateSettingsAsync(string userId, UpdateUserSettingsRequest request)
        {
            var spec = new UserSettingsByUserSpec(userId);
            var settings = await _unitOfWork.Repository<UserSettings, int>()
                                            .GetEntityWithSpecAsync(spec);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    Language = request.Language ?? LaundryShared.Enums.CustomerEnums.Language.Arabic,
                    Currency = request.Currency ?? LaundryShared.Enums.CustomerEnums.Currency.EGP,
                    PushNotifications = request.PushNotifications ?? true,
                    EmailNotifications = request.EmailNotifications ?? true,
                    SmsNotifications = request.SmsNotifications ?? true,
                    WhatsappNotifications = request.WhatsappNotifications ?? true,
                    ShowProfile = request.ShowProfile ?? true,
                    ShareData = request.ShareData ?? false
                };

                await _unitOfWork.Repository<UserSettings, int>().AddAsync(settings);
            }
            else
            {
                if (request.Language.HasValue) settings.Language = request.Language.Value;
                if (request.Currency.HasValue) settings.Currency = request.Currency.Value;
                if (request.PushNotifications.HasValue) settings.PushNotifications = request.PushNotifications.Value;
                if (request.EmailNotifications.HasValue) settings.EmailNotifications = request.EmailNotifications.Value;
                if (request.SmsNotifications.HasValue) settings.SmsNotifications = request.SmsNotifications.Value;
                if (request.WhatsappNotifications.HasValue) settings.WhatsappNotifications = request.WhatsappNotifications.Value;
                if (request.ShowProfile.HasValue) settings.ShowProfile = request.ShowProfile.Value;
                if (request.ShareData.HasValue) settings.ShareData = request.ShareData.Value;
                settings.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<UserSettings, int>().Update(settings);
            }

            await _unitOfWork.CompleteAsync();

            return Result<UserSettingsDto>.Success(new UserSettingsDto
            {
                Language = settings.Language,
                Currency = settings.Currency,
                PushNotifications = settings.PushNotifications,
                EmailNotifications = settings.EmailNotifications,
                SmsNotifications = settings.SmsNotifications,
                WhatsappNotifications = settings.WhatsappNotifications,
                ShowProfile = settings.ShowProfile,
                ShareData = settings.ShareData
            });
        }

        // Private Helpers
        private async Task ResetDefaultAddressAsync(string userId)
        {
            var spec = new UserAddressesByUserSpec(userId);
            var addresses = await _unitOfWork.Repository<UserAddress, int>()
                                             .GetAllWithSpecAsync(spec);

            foreach (var address in addresses.Where(a => a.IsDefault))
            {
                address.IsDefault = false;
                _unitOfWork.Repository<UserAddress, int>().Update(address);
            }
        }

        private async Task ResetDefaultPaymentMethodAsync(string userId)
        {
            var spec = new UserPaymentMethodsByUserSpec(userId);
            var methods = await _unitOfWork.Repository<UserPaymentMethod, int>()
                                           .GetAllWithSpecAsync(spec);

            foreach (var method in methods.Where(m => m.IsDefault))
            {
                method.IsDefault = false;
                _unitOfWork.Repository<UserPaymentMethod, int>().Update(method);
            }
        }

        private CardBrand DetectCardBrand(string cardNumber)
        {
            if (cardNumber.StartsWith("4")) return CardBrand.Visa;
            if (cardNumber.StartsWith("5")) return CardBrand.Mastercard;
            if (cardNumber.StartsWith("3")) return CardBrand.AmericanExpress;
            return CardBrand.Other;
        }

        private AddressDto MapToAddressDto(UserAddress a) => new AddressDto
        {
            Id = a.Id,
            Type = a.Type,
            Label = a.Label,
            Street = a.Street,
            Apt = a.Apt,
            City = a.City,
            Area = a.Area,
            Instructions = a.Instructions,
            IsDefault = a.IsDefault,
            Latitude = a.Latitude,
            Longitude = a.Longitude
        };

        private UserPaymentMethodDto MapToPaymentMethodDto(UserPaymentMethod m) => new UserPaymentMethodDto
        {
            Id = m.Id,
            Type = m.Type,
            CardLast4 = m.CardLast4,
            CardBrand = m.CardBrand,
            ExpiryMonth = m.ExpiryMonth,
            ExpiryYear = m.ExpiryYear,
            CardholderName = m.CardholderName,
            IsDefault = m.IsDefault,
            CreatedAt = m.CreatedAt
        };
    }
}
