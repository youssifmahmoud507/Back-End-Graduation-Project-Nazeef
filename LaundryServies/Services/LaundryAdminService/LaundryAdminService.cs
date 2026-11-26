using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.LaundryAdminIdSpec;
using LaundryServies.Common;
using LaundryServiesAbstraction.IServices.LaundryAdmin;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LaundryServies.Services.LaundryAdminService
{
    /// <summary>
    /// Service for the laundry owner.
    /// Handles their orders, services, and laundry only.
    /// </summary>
    public class LaundryAdminService : ILaundryAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IKashierService _kashierService;

        public LaundryAdminService(
            IUnitOfWork unitOfWork, 
            IFileService fileService, 
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IKashierService kashierService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _notificationService = notificationService;
            _userManager = userManager;
            _kashierService = kashierService;
        }

        // ============================================================
        // Setup/Register Laundry Profile
        // ============================================================
        
        public async Task<Result<bool>> SetupLaundryProfileAsync(string adminId, SetupLaundryRequest request)
        {
            try
            {
                var existingLaundry = await _unitOfWork.Repository<Laundry, int>()
                    .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

                if (existingLaundry != null)
                    return Result<bool>.Failure("You already have a laundry registered to this account.");

                var laundry = new Laundry
                {
                    Name = request.Name,
                    Address = request.Address,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    AdminId = adminId,
                    Status = LaundryShared.Enums.CustomerEnums.LaundryStatus.Inactive, // Inactive pending admin approval
                    Availability = LaundryShared.Enums.CustomerEnums.LaundryAvailability.Closed, // Initial state
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Laundry, int>().AddAsync(laundry);
                await _unitOfWork.CompleteAsync();

                return Result<bool>.Success(true);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Laundries_Users_AdminId") == true)
            {
                return Result<bool>.Failure("Your user account could not be found in the database. Please log out, clear your local storage, and log back in.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to create profile: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }

        public async Task<Result<bool>> UpdateLaundryProfileAsync(string adminId, SetupLaundryRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            laundry.Name = request.Name;
            laundry.Address = request.Address;
            laundry.Latitude = request.Latitude;
            laundry.Longitude = request.Longitude;
            laundry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Laundry, int>().Update(laundry);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<LaundryProfileDto>> GetLaundryProfileAsync(string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<LaundryProfileDto>.Failure("No laundry is linked to this account.");

            var dto = new LaundryProfileDto
            {
                Id = laundry.Id,
                Name = laundry.Name,
                Address = laundry.Address,
                Latitude = laundry.Latitude,
                Longitude = laundry.Longitude,
                Status = laundry.Status,
                Availability = laundry.Availability,
                ImageUrl = laundry.ImageUrl,
                AverageRating = laundry.AverageRating,
                TotalReviews = laundry.TotalReviews,
                CreatedAt = laundry.CreatedAt
            };

            return Result<LaundryProfileDto>.Success(dto);
        }

        public async Task<Result<List<LaundryServiceDto>>> GetLaundryServicesAsync(string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<List<LaundryServiceDto>>.Failure("No laundry is linked to this account.");

            var servicesDto = (laundry.Services ?? new List<Service>()).Select(s => new LaundryServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Category = s.Category.ToString(),
                Price = s.Price,
                IsAvailable = s.IsAvailable
            }).ToList();

            return Result<List<LaundryServiceDto>>.Success(servicesDto);
        }


        // ============================================================
        // Order Management
        // ============================================================

        public async Task<Result<List<LaundryOrderDto>>> GetIncomingOrdersAsync(string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<List<LaundryOrderDto>>.Failure("No laundry is linked to this account.");

            var orders = await _unitOfWork.Repository<Order, int>()
                .GetAllWithSpecAsync(new OrdersByLaundryIdSpec(laundry.Id));

            var result = orders.Select(o => new LaundryOrderDto
            {
                Id = o.Id,
                CustomerName = o.Customer?.Name ?? string.Empty,
                CustomerPhone = o.Customer?.PhoneNumber ?? string.Empty,
                PickupLocation = o.PickupLocation,
                DeliveryLocation = o.DeliveryLocation,
                PickupTime = o.PickupTime,
                Status = o.Status,
                TotalPrice = o.TotalPrice,
                CreatedAt = o.CreatedAt,
                Items = (o.OrderItems ?? new List<OrderItem>()).Select(i => new LaundryOrderItemDto
                {
                    ServiceName = i.Service?.ServiceName ?? string.Empty,
                    Quantity = i.Quantity,
                    Subtotal = i.Subtotal
                }).ToList()
            }).ToList();

            return Result<List<LaundryOrderDto>>.Success(result);
        }

        public async Task<Result<LaundryOrderDto>> GetOrderByIdAsync(int orderId, string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<LaundryOrderDto>.Failure("No laundry is linked to this account.");

            var order = await _unitOfWork.Repository<Order, int>()
                .GetEntityWithSpecAsync(new OrderByIdWithDetailsSpec(orderId));

            if (order is null || order.LaundryId != laundry.Id)
                return Result<LaundryOrderDto>.Failure("Order not found or does not belong to your laundry.");

            var dto = new LaundryOrderDto
            {
                Id = order.Id,
                CustomerName = order.Customer?.Name ?? string.Empty,
                CustomerPhone = order.Customer?.PhoneNumber ?? string.Empty,
                PickupLocation = order.PickupLocation,
                DeliveryLocation = order.DeliveryLocation,
                PickupTime = order.PickupTime,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                CreatedAt = order.CreatedAt,
                Items = (order.OrderItems ?? new List<OrderItem>()).Select(i => new LaundryOrderItemDto
                {
                    ServiceName = i.Service?.ServiceName ?? string.Empty,
                    Quantity = i.Quantity,
                    Subtotal = i.Subtotal
                }).ToList()
            };

            return Result<LaundryOrderDto>.Success(dto);
        }

        public async Task<Result<bool>> UpdateOrderStatusAsync(int orderId, string adminId, UpdateOrderStatusRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            var spec = new OrderByIdWithDetailsSpec(orderId);
            var order = await _unitOfWork.Repository<Order, int>()
                .GetEntityWithSpecAsync(spec);

            if (order is null || order.LaundryId != laundry.Id)
                return Result<bool>.Failure("Order not found or does not belong to your laundry.");

            if (request.NewStatus == OrderStatus.Cancelled &&
                string.IsNullOrWhiteSpace(request.CancellationReason))
                return Result<bool>.Failure("A cancellation reason is required.");

            if (request.NewStatus == OrderStatus.Cancelled)
            {
                if (order.Payment != null && order.Payment.PaymentStatus == PaymentStatus.Paid && order.Payment.PaymentMethod != PaymentMethod.Cash)
                {
                    var refundResult = await _kashierService.RefundWalletPaymentAsync(order.CustomerId, order.Id, order.Payment.Amount);
                    if (!refundResult.IsSuccess)
                    {
                        return Result<bool>.Failure("Error refunding payment: " + refundResult.ErrorMessage);
                    }
                }
                order.CancellationReason = request.CancellationReason;
            }

            order.Status = request.NewStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            if (request.NewStatus == OrderStatus.ReadyForPickup)
            {
                await _notificationService.SendOrderReadyAsync(order.CustomerId, order.Id);
            }

            return Result<bool>.Success(true);
        }

        // ============================================================
        // COURIER ASSIGNMENT & DISPATCH
        // ============================================================

        public async Task<Result<bool>> AssignCourierToOrderExplicitlyAsync(string adminId, AssignCourierRequestDto request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null) return Result<bool>.Failure("No laundry found.");

            var order = await _unitOfWork.Repository<Order, int>().GetByIdAsync(request.OrderId);
            if (order is null || order.LaundryId != laundry.Id) 
                return Result<bool>.Failure("Order not found or not yours.");

            ApplicationUser? courierUser = null;

            if (!string.IsNullOrEmpty(request.CourierPhone))
            {
                // Assign by Phone
                var allUsers = _userManager.Users.ToList(); // Note: Replace with proper query via spec in real system to avoid pulling all users
                courierUser = allUsers.FirstOrDefault(x => x.PhoneNumber == request.CourierPhone);
            }
            else if (!string.IsNullOrEmpty(request.CourierId))
            {
                courierUser = await _userManager.FindByIdAsync(request.CourierId);
            }

            if (courierUser is null) return Result<bool>.Failure("Courier not found with provided phone/ID.");

            // Update Order
            order.CourierId = courierUser.Id;
            if (order.Status < OrderStatus.Accepted)
            {
                order.Status = OrderStatus.Accepted;
            }
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            // Notify Customer directly
            string eta = "15 mins"; // Could be dynamically calculated based on distance
            await _notificationService.SendCourierAssignedToCustomerAsync(
                order.CustomerId, order.Id, courierUser.Name, courierUser.PhoneNumber ?? "No Phone Number", eta);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<AvailableCourierDto>>> SuggestOrderToAvailableCouriersAsync(string adminId, BroadcastOrderRequestDto request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null) return Result<List<AvailableCourierDto>>.Failure("No laundry found.");

            var order = await _unitOfWork.Repository<Order, int>().GetByIdAsync(request.OrderId);
            if (order is null || order.LaundryId != laundry.Id)
                return Result<List<AvailableCourierDto>>.Failure("Order not found or not yours.");

            // In a real system, query CourierProfile table by coordinate radius.
            // Mocking for integration:
            var availableCouriers = new List<AvailableCourierDto>
            {
                new AvailableCourierDto { CourierId = "c1", Name = "Ahmed Ragab", Phone = "+201011112222", DistanceKm = 1.2, Rating = 4.9 },
                new AvailableCourierDto { CourierId = "c2", Name = "Khaled Mansour", Phone = "+201012345678", DistanceKm = 2.5, Rating = 4.8 }
            };

            // Notify all of them via SignalR/DB that an order exists
            var courierIds = availableCouriers.Select(x => x.CourierId).ToList();
            await _notificationService.SendOrderBroadcastToCouriersAsync(
                courierIds, order.Id, laundry.Name, order.PickupLocation, order.DeliveryLocation);

            // Returning the list so the Laundry Admin can see who was notified
            return Result<List<AvailableCourierDto>>.Success(availableCouriers);
        }

        // ============================================================
        // Laundry Management
        // ============================================================

        public async Task<Result<bool>> UpdateAvailabilityAsync(string adminId, UpdateLaundryAvailabilityRequest request)
        {
            if (!Enum.IsDefined(request.Availability))
                return Result<bool>.Failure("Invalid laundry availability value.");

            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            laundry.Availability = request.Availability;
            laundry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Laundry, int>().Update(laundry);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // ============================================================
        // Service Management
        // ============================================================

        public async Task<Result<bool>> AddServiceAsync(string adminId, AddServiceRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            var normalizedName = request.ServiceName?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                return Result<bool>.Failure("Service name is required.");

            if (request.Price <= 0)
                return Result<bool>.Failure("Price must be greater than 0.");

            var duplicateExists = (laundry.Services ?? new List<Service>())
                .Any(s =>
                    s.Category == request.Category &&
                    string.Equals(s.ServiceName, normalizedName, System.StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
                return Result<bool>.Failure("This service already exists in your catalog.");

            var service = new Service
            {
                ServiceName = normalizedName,
                Category = request.Category,
                Price = request.Price,
                IsAvailable = request.IsAvailable,
                LaundryId = laundry.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Service, int>().AddAsync(service);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> UpdateServiceAsync(int serviceId, string adminId, UpdateServiceRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            var service = await _unitOfWork.Repository<Service, int>()
                .GetByIdAsync(serviceId);

            if (service is null || service.LaundryId != laundry.Id)
                return Result<bool>.Failure("Service not found or does not belong to your laundry.");

            if (request.ServiceName is not null)
                service.ServiceName = request.ServiceName;

            if (request.Category is not null)
                service.Category = request.Category.Value;

            if (request.Price is not null)
                service.Price = request.Price.Value;

            if (request.IsAvailable is not null)
                service.IsAvailable = request.IsAvailable.Value;

            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service, int>().Update(service);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteServiceAsync(int serviceId, string adminId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<bool>.Failure("No laundry is linked to this account.");

            var service = await _unitOfWork.Repository<Service, int>()
                .GetByIdAsync(serviceId);

            if (service is null || service.LaundryId != laundry.Id)
                return Result<bool>.Failure("Service not found or does not belong to your laundry.");

            _unitOfWork.Repository<Service, int>().Delete(service);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // ============================================================
        // Image Management
        // ============================================================

        public async Task<Result<string>> UploadLaundryImageAsync(string adminId, IFormFile file)
        {
            // Benjib elghoslah beta'tuh
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                .GetEntityWithSpecAsync(new LaundryByAdminIdSpec(adminId));

            if (laundry is null)
                return Result<string>.Failure("No laundry is linked to this account");

            // Bent'akad enn el file sora
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return Result<string>.Failure("Only image files are allowed (jpg, jpeg, png, webp)");

            // Bent'akad enn el hasseb mesh akbar men 5MB
            if (file.Length > 5 * 1024 * 1024)
                return Result<string>.Failure("Image size must not exceed 5MB");

            // Benrawwah el sora aala Cloudinary
            var imageUrl = await _fileService.UploadImageAsync(file, "laundries");

            // Law fe sota qadima bnamsiha men Cloudinary
            if (!string.IsNullOrEmpty(laundry.ImageUrl))
            {
                // Benesta'rodh el PublicId men el URL el qadeem
                var oldPublicId = GetPublicIdFromUrl(laundry.ImageUrl);
                if (!string.IsNullOrEmpty(oldPublicId))
                    await _fileService.DeleteImageAsync(oldPublicId);
            }

            // Benhafez el URL el jadeed fi el data base
            laundry.ImageUrl = imageUrl;
            laundry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Laundry, int>().Update(laundry);
            await _unitOfWork.CompleteAsync();

            return Result<string>.Success(imageUrl);
        }

        // Helper byestra'ad el PublicId men el Cloudinary URL
        private string GetPublicIdFromUrl(string imageUrl)
        {
            try
            {
                // El URL beybe sharro:
                // https://res.cloudinary.com/doml9myki/image/upload/v123/nadeef/laundries/filename.webp
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");

                if (uploadIndex == -1) return string.Empty;

                // Banakhod kol haga ba'd el version (v123)
                var publicIdParts = segments.Skip(uploadIndex + 2);
                var publicId = string.Join("/", publicIdParts);

                // Bneesh el Extension
                return Path.ChangeExtension(publicId, null);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
