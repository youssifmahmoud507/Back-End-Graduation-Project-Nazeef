using LaundryDomain.IRepositories;
using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.CourierSpecs;
using LaundryServiesAbstraction.IServices.Courier;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.Courier;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.Courier
{
    public class CourierService : ICourierService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IKashierService _kashierService;

        private const double PickupRadiusKm = 0.5;
        private const double DeliveryRadiusKm = 0.5;
        private const double NearbyAlertKm = 1.0;
        private const int MaxCancellations = 5;
        private const decimal DeliveryFeePerOrder = FinancialConstants.DeliveryFee;

        public CourierService(
            IUnitOfWork uow,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IKashierService kashierService)
        {
            _uow = uow;
            _userManager = userManager;
            _notificationService = notificationService;
            _kashierService = kashierService;
        }

        // ============================================================
        // 1. GET ASSIGNED ORDERS
        // ============================================================
        public async Task<Pagination<CourierOrderDto>> GetAssignedOrdersAsync(
            string courierId, CourierOrderParams orderParams)
        {
            int skip = (orderParams.PageIndex - 1) * orderParams.PageSize;

            var spec = new CourierOrdersSpec(courierId, orderParams.Filter, skip, orderParams.PageSize);
            var countSpec = new CourierOrdersCountSpec(courierId, orderParams.Filter);

            var orders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(spec);
            var total = await _uow.Repository<Order, int>().CountAsync(countSpec);
            var dtos = orders.Select(MapToDto).ToList();

            return new Pagination<CourierOrderDto>(orderParams.PageIndex, orderParams.PageSize, total, dtos);
        }

        // ============================================================
        // 1.5. GET AVAILABLE ORDERS (unassigned, for this courier's laundry)
        // ============================================================
        public async Task<Result<Pagination<CourierOrderDto>>> GetAvailableOrdersAsync(
            string courierId, int pageIndex, int pageSize)
        {
            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile?.LaundryId == null)
                return Result<Pagination<CourierOrderDto>>.Failure("Courier is not assigned to a laundry yet");

            int skip = (pageIndex - 1) * pageSize;
            var spec = new AvailableOrdersForCourierSpec(profile.LaundryId.Value, skip, pageSize);
            var countSpec = new AvailableOrdersForCourierCountSpec(profile.LaundryId.Value);

            var orders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(spec);
            var total = await _uow.Repository<Order, int>().CountAsync(countSpec);
            var dtos = orders.Select(MapToDto).ToList();

            return Result<Pagination<CourierOrderDto>>.Success(
                new Pagination<CourierOrderDto>(pageIndex, pageSize, total, dtos));
        }

        // ============================================================
        // 2. ACCEPT ORDER
        // ============================================================
        public async Task<Result<CourierOrderDto>> AcceptOrderAsync(string courierId, int orderId)
        {
            var spec = new PendingOrderForAcceptSpec(orderId);
            var order = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<CourierOrderDto>.Failure("Order not found or already taken by another courier");

            var activeSpec = new CourierActiveOrderSpec(courierId);
            var activeOrder = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(activeSpec);

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile?.LaundryId == null)
                return Result<CourierOrderDto>.Failure("Courier is not assigned to a laundry yet");

            if (activeOrder != null)
                return Result<CourierOrderDto>.Failure("You have a conflicting active order");

            order.CourierId = courierId;
            // لو الأوردر كان Accepted (اللاندري قبلته ولسه محدش أخده) → حوّله لـ ReadyForPickup
            // لو كان ReadyForPickup بالفعل → افضل كما هو
            if (order.Status == OrderStatus.Accepted)
                order.Status = OrderStatus.ReadyForPickup;
            order.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<Order, int>().Update(order);
            await _uow.CompleteAsync();

            await _notificationService.SendNotificationAsync(
                order.CustomerId,
                "Order Assigned",
                "A courier has been assigned to your order");

            return Result<CourierOrderDto>.Success(MapToDto(order));
        }

        // ============================================================
        // 3. UPDATE LOCATION
        // ============================================================
        public async Task<Result<UpdateLocationResponse>> UpdateLocationAsync(
            string courierId, UpdateLocationRequest request)
        {
            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
                return Result<UpdateLocationResponse>.Failure("Invalid location data");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile == null)
            {
                profile = new CourierProfile
                {
                    CourierId = courierId,
                    CurrentLatitude = request.Latitude,
                    CurrentLongitude = request.Longitude,
                    LastLocationUpdate = DateTime.UtcNow
                };
                await _uow.Repository<CourierProfile, int>().AddAsync(profile);
            }
            else
            {
                profile.CurrentLatitude = request.Latitude;
                profile.CurrentLongitude = request.Longitude;
                profile.LastLocationUpdate = DateTime.UtcNow;
                _uow.Repository<CourierProfile, int>().Update(profile);
            }

            var activeSpec = new CourierActiveOrderSpec(courierId);
            var activeOrder = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(activeSpec);

            double? distanceKm = null;
            DateTime? eta = null;
            bool nearDest = false;

            if (activeOrder != null)
            {
                activeOrder.CourierLatitude = request.Latitude;
                activeOrder.CourierLongitude = request.Longitude;

                if (activeOrder.Laundry != null)
                {
                    distanceKm = CalculateDistance(
                        request.Latitude,
                        request.Longitude,
                        (double)activeOrder.Laundry.Latitude,
                        (double)activeOrder.Laundry.Longitude);

                    var minutesEta = (distanceKm.Value / 30.0) * 60;
                    eta = DateTime.UtcNow.AddMinutes(minutesEta);

                    activeOrder.EstimatedDeliveryTime = eta;
                    nearDest = distanceKm <= NearbyAlertKm;
                }

                _uow.Repository<Order, int>().Update(activeOrder);

                if (nearDest)
                {
                    await _notificationService.SendNotificationAsync(
                        activeOrder.CustomerId,
                        "Courier Nearby",
                        "Your courier is nearby!");
                }
            }

            await _uow.CompleteAsync();

            return Result<UpdateLocationResponse>.Success(new UpdateLocationResponse
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                UpdatedAt = DateTime.UtcNow,
                DistanceToDestination = distanceKm,
                EstimatedArrival = eta,
                NearDestination = nearDest
            });
        }

        // ============================================================
        // 4. CONFIRM PICKUP
        // ============================================================
        public async Task<Result<CourierOrderDto>> ConfirmPickupAsync(
            string courierId, int orderId, ConfirmPickupRequest request)
        {
            var spec = new CourierOrderByIdSpec(orderId);
            var order = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<CourierOrderDto>.Failure("Order not found");

            if (order.CourierId != courierId)
                return Result<CourierOrderDto>.Failure("This order is not assigned to you");

            if (order.Status != OrderStatus.ReadyForPickup)
                return Result<CourierOrderDto>.Failure("Order is not ready for pickup");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile?.LaundryId != null && order.LaundryId != profile.LaundryId.Value)
                return Result<CourierOrderDto>.Failure("This order does not belong to your assigned laundry");

            if (order.Laundry != null)
            {
                var dist = CalculateDistance(
                    request.CurrentLatitude,
                    request.CurrentLongitude,
                    (double)order.Laundry.Latitude,
                    (double)order.Laundry.Longitude);

                if (dist > PickupRadiusKm)
                    return Result<CourierOrderDto>.Failure("You must be at the laundry to confirm pickup");
            }

            order.Status = OrderStatus.PickedUp;
            order.PickedUpAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<Order, int>().Update(order);
            await _uow.CompleteAsync();

            await _notificationService.SendOrderInProgressAsync(order.CustomerId, order.Id);

            return Result<CourierOrderDto>.Success(MapToDto(order));
        }

        // ============================================================
        // 5. CONFIRM DELIVERY
        // ============================================================
        public async Task<Result<CourierOrderDto>> ConfirmDeliveryAsync(
            string courierId, int orderId, ConfirmDeliveryRequest request)
        {
            var spec = new CourierOrderByIdSpec(orderId);
            var order = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<CourierOrderDto>.Failure("Order not found");

            if (order.CourierId != courierId)
                return Result<CourierOrderDto>.Failure("This order is not assigned to you");

            if (order.Status != OrderStatus.PickedUp)
                return Result<CourierOrderDto>.Failure("Order has not been picked up yet");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile?.LaundryId != null && order.LaundryId != profile.LaundryId.Value)
                return Result<CourierOrderDto>.Failure("This order does not belong to your assigned laundry");

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<Order, int>().Update(order);

            if (profile != null)
            {
                profile.CompletedOrdersCount++;
                _uow.Repository<CourierProfile, int>().Update(profile);
            }

            await _uow.CompleteAsync();

            await _notificationService.SendOrderDeliveredAsync(order.CustomerId, order.Id);

            return Result<CourierOrderDto>.Success(MapToDto(order));
        }

        // ============================================================
        // 6. CANCEL ORDER
        // ============================================================
        public async Task<Result<bool>> CancelOrderAsync(
            string courierId, int orderId, CourierCancelOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CancellationReason))
                return Result<bool>.Failure("Cancellation reason is required");

            var spec = new CourierOrderByIdSpec(orderId);
            var order = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<bool>.Failure("Order not found");

            if (order.CourierId != courierId)
                return Result<bool>.Failure("This order is not assigned to you");

            if (order.Status != OrderStatus.ReadyForPickup && order.Status != OrderStatus.Accepted)
                return Result<bool>.Failure("Order cannot be cancelled at this stage");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile != null && profile.CancellationCount >= MaxCancellations)
                return Result<bool>.Failure("Cancellation limit reached, please contact support");

            if (order.Payment != null && order.Payment.PaymentStatus == PaymentStatus.Paid && order.Payment.PaymentMethod != PaymentMethod.Cash)
            {
                var refundResult = await _kashierService.RefundWalletPaymentAsync(order.CustomerId, order.Id, order.Payment.Amount);
                if (!refundResult.IsSuccess)
                {
                    return Result<bool>.Failure("Error refunding payment: " + refundResult.ErrorMessage);
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = request.CancellationReason;
            order.CourierId = null;
            order.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<Order, int>().Update(order);

            if (profile != null)
            {
                profile.CancellationCount++;
                _uow.Repository<CourierProfile, int>().Update(profile);
            }

            await _uow.CompleteAsync();

            await _notificationService.SendOrderCancelledAsync(order.CustomerId, order.Id, request.CancellationReason);

            return Result<bool>.Success(true);
        }

        // ============================================================
        // 7. DELIVERY HISTORY
        // ============================================================
        public async Task<Pagination<DeliveryHistoryDto>> GetDeliveryHistoryAsync(
            string courierId, int pageIndex, int pageSize)
        {
            int skip = (pageIndex - 1) * pageSize;

            var spec = new CourierDeliveryHistorySpec(courierId, skip, pageSize);
            var countSpec = new CourierDeliveryHistoryCountSpec(courierId);

            var orders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(spec);
            var total = await _uow.Repository<Order, int>().CountAsync(countSpec);

            var dtos = orders.Select(o => new DeliveryHistoryDto
            {
                OrderId = o.Id,
                CustomerName = o.Customer?.Name ?? "",
                DeliveryLocation = o.DeliveryLocation,
                DeliveredAt = o.DeliveredAt,
                TotalPrice = o.TotalPrice,
                EarningsFromOrder = DeliveryFeePerOrder,
                FinalStatus = o.Status
            }).ToList();

            return new Pagination<DeliveryHistoryDto>(pageIndex, pageSize, total, dtos);
        }

        // ============================================================
        // 8. STATS & EARNINGS
        // ============================================================
        public async Task<Result<CourierStatsDto>> GetStatsAsync(string courierId)
        {
            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var allOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(new CourierEarningsSpec(courierId, null, null));
            var todayOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(new CourierEarningsSpec(courierId, todayStart, now));
            var weekOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(new CourierEarningsSpec(courierId, weekStart, now));
            var monthOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(new CourierEarningsSpec(courierId, monthStart, now));

            int completed = profile?.CompletedOrdersCount ?? allOrders.Count;
            int cancellations = profile?.CancellationCount ?? 0;
            int total = completed + cancellations;

            return Result<CourierStatsDto>.Success(new CourierStatsDto
            {
                TotalCompletedOrders = completed,
                TotalCancellations = cancellations,
                TotalEarnings = allOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder,
                TodayEarnings = todayOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder,
                WeekEarnings = weekOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder,
                MonthEarnings = monthOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder,
                CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 100
            });
        }

        // ============================================================
        // 9. PROFILE
        // ============================================================
        public async Task<Result<CourierProfileDto>> GetProfileAsync(string courierId)
        {
            var user = await _userManager.FindByIdAsync(courierId);
            if (user == null)
                return Result<CourierProfileDto>.Failure("Courier not found");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            return Result<CourierProfileDto>.Success(new CourierProfileDto
            {
                Name = user.Name,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                IsAvailable = profile?.IsAvailable ?? true,
                CompletedOrdersCount = profile?.CompletedOrdersCount ?? 0,
                CancellationCount = profile?.CancellationCount ?? 0,
                LaundryId = profile?.LaundryId,
                LaundryName = profile?.Laundry?.Name,
                CurrentLatitude = profile?.CurrentLatitude,
                CurrentLongitude = profile?.CurrentLongitude,
                LastLocationUpdate = profile?.LastLocationUpdate
            });
        }

        public async Task<Result<CourierProfileDto>> UpdateProfileAsync(
            string courierId, UpdateCourierProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(courierId);
            if (user == null)
                return Result<CourierProfileDto>.Failure("Courier not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            await _userManager.UpdateAsync(user);

            if (request.IsAvailable.HasValue)
            {
                var profileSpec = new CourierProfileByUserIdSpec(courierId);
                var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

                if (profile == null)
                {
                    profile = new CourierProfile
                    {
                        CourierId = courierId,
                        IsAvailable = request.IsAvailable.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _uow.Repository<CourierProfile, int>().AddAsync(profile);
                }
                else
                {
                    profile.IsAvailable = request.IsAvailable.Value;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _uow.Repository<CourierProfile, int>().Update(profile);
                }

                await _uow.CompleteAsync();
            }

            return await GetProfileAsync(courierId);
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private static double CalculateDistance(
            double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

        private static CourierOrderDto MapToDto(Order o) => new CourierOrderDto
        {
            Id = o.Id,
            PickupLocation = o.PickupLocation,
            DeliveryLocation = o.DeliveryLocation,
            PickupTime = o.PickupTime,
            DeliveryTime = o.DeliveryTime,
            EstimatedDeliveryTime = o.EstimatedDeliveryTime,
            Status = o.Status,
            TotalPrice = o.TotalPrice,
            Notes = o.Notes,
            CustomerName = o.Customer?.Name ?? "",
            CustomerPhone = o.Customer?.PhoneNumber ?? "",
            LaundryName = o.Laundry?.Name ?? "",
            LaundryAddress = o.Laundry?.Address ?? "",
            LaundryLatitude = o.Laundry?.Latitude,
            LaundryLongitude = o.Laundry?.Longitude,
            PickedUpAt = o.PickedUpAt,
            DeliveredAt = o.DeliveredAt,
            CourierLatitude = o.CourierLatitude,
            CourierLongitude = o.CourierLongitude,
            IsPaid = o.Payment?.PaymentStatus == LaundryShared.Enums.CustomerEnums.PaymentStatus.Paid,
            ItemsCount = o.OrderItems?.Sum(i => i.Quantity) ?? 0,
            ServiceName = o.OrderItems?.FirstOrDefault()?.Service?.ServiceName ?? "Laundry Service"
        };

        // ============================================================
        // APP REQUIREMENTS
        // ============================================================

        public async Task<Result<CourierProfileResponseDto>> GetAppProfileAsync(string courierId)
        {
            var user = await _userManager.FindByIdAsync(courierId);
            if (user == null) return Result<CourierProfileResponseDto>.Failure("Courier not found");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            // Mock calculations for missing tables/timesheets
            int activeHours = 412; 
            int cancelled = profile?.CancellationCount ?? 0;
            int completed = profile?.CompletedOrdersCount ?? 0;
            double completionRate = completed + cancelled > 0 ? ((double)completed / (completed + cancelled)) * 100 : 100;
            string monthlyEarningsMock = "7.2k"; 

            return Result<CourierProfileResponseDto>.Success(new CourierProfileResponseDto
            {
                Id = courierId,
                Name = user.Name,
                Phone = user.PhoneNumber ?? "",
                Avatar = string.Join("", user.Name.Split().Take(2).Select(n => n.FirstOrDefault().ToString())),
                Rating = 4.8, // Mock Rating
                TotalOrders = completed,
                CompletionRate = Math.Round(completionRate, 1),
                ActiveHours = activeHours,
                IsOnline = profile?.IsAvailable ?? false,
                Availability = (profile?.IsAvailable ?? false) ? "online" : "offline",
                UnreadNotifications = 2, // Mock Notifications
                AssignedLaundry = profile?.Laundry != null ? new CourierAssignedLaundryDto
                {
                    Name = profile.Laundry.Name,
                    Address = profile.Laundry.Address ?? "Multiple Locations",
                    Status = "Active"
                } : null,
                Performance = new CourierPerformanceOverviewDto
                {
                    Completed = completed,
                    Cancelled = cancelled,
                    Hours = activeHours,
                    EarningsThisMonth = monthlyEarningsMock
                }
            });
        }

        public async Task<Result<bool>> UpdateStatusAsync(string courierId, UpdateCourierStatusRequestDto request)
        {
            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile == null)
            {
                profile = new CourierProfile
                {
                    CourierId = courierId,
                    IsAvailable = request.IsOnline,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Repository<CourierProfile, int>().AddAsync(profile);
            }
            else
            {
                profile.IsAvailable = request.IsOnline;
                profile.UpdatedAt = DateTime.UtcNow;
                _uow.Repository<CourierProfile, int>().Update(profile);
            }

            await _uow.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<CourierStatsResponseDto>> GetTodaysStatsAsync(string courierId)
        {
            var today = DateTime.UtcNow.Date;
            var todayOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(new CourierEarningsSpec(courierId, today, DateTime.UtcNow));

            return Result<CourierStatsResponseDto>.Success(new CourierStatsResponseDto
            {
                TodaysOrders = todayOrders.Count,
                InTransit = todayOrders.Count(o => o.Status == OrderStatus.PickedUp),
                EarnedToday = todayOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder,
                VsYesterdayPercentage = 12.0 // Mock Percentage
            });
        }

        public async Task<Result<CourierActiveRunResponseDto>> GetActiveRunAsync(string courierId)
        {
            // Get active orders for this courier (PickedUp = in transit)
            var activeSpec = new CourierOrdersSpec(courierId, LaundryShared.Enums.CustomerEnums.CourierOrderFilter.InProgress, 0, 50);
            var activeOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(activeSpec);

            // Also include ReadyForPickup orders (accepted but not yet picked)
            var pendingSpec = new CourierOrdersSpec(courierId, LaundryShared.Enums.CustomerEnums.CourierOrderFilter.Pending, 0, 50);
            var pendingOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(pendingSpec);

            var allRunOrders = pendingOrders.Concat(activeOrders).OrderBy(o => o.PickupTime).ToList();

            if (!allRunOrders.Any())
                return Result<CourierActiveRunResponseDto>.Failure("No active run found");

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            var laundryName = allRunOrders.FirstOrDefault()?.Laundry?.Name ?? "Ndeef Laundry";
            int totalItems = allRunOrders.Sum(o => o.OrderItems?.Sum(i => i.Quantity) ?? 0);
            decimal totalAmount = allRunOrders.Sum(o => o.TotalPrice);
            int stopsDone = activeOrders.Count(o => o.Status == OrderStatus.Delivered);
            int deliveredCount = activeOrders.Count(o => o.Status == OrderStatus.Delivered);
            decimal earnedSoFar = deliveredCount * DeliveryFeePerOrder;

            var stops = allRunOrders.Select((o, idx) =>
            {
                string status = o.Status == OrderStatus.Delivered ? "done"
                    : o.Status == OrderStatus.PickedUp ? "current"
                    : idx == 0 ? "current"
                    : "upcoming";

                bool isPaid = o.Payment?.PaymentStatus == LaundryShared.Enums.CustomerEnums.PaymentStatus.Paid;
                string serviceName = o.OrderItems?.FirstOrDefault()?.Service?.ServiceName ?? "Laundry Service";
                string customerPhone = o.Customer?.PhoneNumber ?? "";
                string area = o.DeliveryLocation?.Split(',').FirstOrDefault()?.Trim() ?? "Unknown";

                // ETA: rough estimate based on stop index (5 min per stop)
                string eta = status == "done" ? "Done"
                    : status == "current" ? "Now"
                    : $"{(idx + 1) * 15} min";

                return new CourierActiveRunStopDto
                {
                    StopNum = idx + 1,
                    OrderId = o.Id.ToString(),
                    Customer = o.Customer?.Name ?? "Customer",
                    Phone = customerPhone,
                    Area = area,
                    Address = o.DeliveryLocation ?? "",
                    Amount = o.TotalPrice,
                    Items = o.OrderItems?.Sum(i => i.Quantity) ?? 0,
                    Service = serviceName,
                    EtaFromNow = eta,
                    Note = o.Notes ?? "",
                    Status = status,
                    IsUrgent = false,
                    IsPaid = isPaid
                };
            }).ToList();

            return Result<CourierActiveRunResponseDto>.Success(new CourierActiveRunResponseDto
            {
                RunId = $"run_{courierId}_{DateTime.UtcNow:yyyyMMdd}",
                TotalDistance = "—",
                TotalAmount = totalAmount,
                StopsDone = stopsDone,
                TotalStops = allRunOrders.Count,
                EarnedSoFar = earnedSoFar,
                RemainingAmount = (allRunOrders.Count - stopsDone) * DeliveryFeePerOrder,
                LaundryName = laundryName,
                TotalItems = totalItems,
                Stops = stops
            });
        }

        public async Task<Result<bool>> DeliverStopAsync(string courierId, string runId, string orderId, DeliverStopRequestDto request)
        {
            if (!int.TryParse(orderId, out int orderIdInt))
                return Result<bool>.Failure("Invalid order ID");

            var spec = new CourierOrderByIdSpec(orderIdInt);
            var order = await _uow.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<bool>.Failure("Order not found");

            if (order.CourierId != courierId)
                return Result<bool>.Failure("This order is not assigned to you");

            if (order.Status == OrderStatus.Delivered)
                return Result<bool>.Success(true); // Already delivered — idempotent

            if (order.Status != OrderStatus.PickedUp && order.Status != OrderStatus.ReadyForPickup)
                return Result<bool>.Failure("Order cannot be delivered at this stage");

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = request.ActualDeliveryTime != default
                ? request.ActualDeliveryTime
                : DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<Order, int>().Update(order);

            // Update courier profile stats
            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);
            if (profile != null)
            {
                profile.CompletedOrdersCount++;
                _uow.Repository<CourierProfile, int>().Update(profile);
            }

            // If cash was collected, mark payment as paid
            if (request.CashCollected && order.Payment != null
                && order.Payment.PaymentStatus != LaundryShared.Enums.CustomerEnums.PaymentStatus.Paid)
            {
                order.Payment.PaymentStatus = LaundryShared.Enums.CustomerEnums.PaymentStatus.Paid;
                order.Payment.PaymentDate = DateTime.UtcNow;
                _uow.Repository<LaundryDomain.Models.OrderModules.Payment, int>().Update(order.Payment);
            }

            await _uow.CompleteAsync();

            await _notificationService.SendOrderDeliveredAsync(order.CustomerId, order.Id);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> SyncLocationAsync(string courierId, LocationSyncRequestDto request)
        {
            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            if (profile != null)
            {
                profile.CurrentLatitude = request.Lat;
                profile.CurrentLongitude = request.Lng;
                profile.LastLocationUpdate = DateTime.UtcNow;
                _uow.Repository<CourierProfile, int>().Update(profile);
                await _uow.CompleteAsync();
            }
            return Result<bool>.Success(true);
        }

        public async Task<Result<CourierEarningsResponseDto>> GetEarningsStatsAsync(string courierId)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            // Week: Mon-Sun
            var weekStart = today.AddDays(-(((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7));

            var profileSpec = new CourierProfileByUserIdSpec(courierId);
            var profile = await _uow.Repository<CourierProfile, int>().GetEntityWithSpecAsync(profileSpec);

            int cancelled = profile?.CancellationCount ?? 0;
            int completed = profile?.CompletedOrdersCount ?? 0;
            double completionRate = completed + cancelled > 0 ? ((double)completed / (completed + cancelled)) * 100 : 100;

            // Real weekly orders
            var weekOrders = await _uow.Repository<Order, int>().GetAllWithSpecAsync(
                new CourierEarningsSpec(courierId, weekStart, today.AddDays(1)));

            int ordersDoneThisWeek = weekOrders.Count(o => o.Status == OrderStatus.Delivered);
            decimal weekEarnings = ordersDoneThisWeek * DeliveryFeePerOrder;
            decimal avgPerOrder = ordersDoneThisWeek > 0 ? DeliveryFeePerOrder : 0;

            // Build daily earnings for each day Mon-Sun
            var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var dailyEarnings = Enumerable.Range(0, 7).Select(i =>
            {
                var day = weekStart.AddDays(i);
                var dayOrders = weekOrders.Where(o => o.DeliveredAt?.Date == day).ToList();
                return new DailyEarningDto
                {
                    Day = dayNames[i],
                    Amount = dayOrders.Count(o => o.Status == OrderStatus.Delivered) * DeliveryFeePerOrder
                };
            }).ToList();

            // Recent transactions (last 10 delivered orders)
            var allDeliveredSpec = new CourierEarningsSpec(courierId, null, null);
            var allDelivered = await _uow.Repository<Order, int>().GetAllWithSpecAsync(allDeliveredSpec);
            var recentTransactions = allDelivered
                .OrderByDescending(o => o.DeliveredAt)
                .Take(10)
                .Select(o =>
                {
                    string timeAgo = FormatTimeAgo(o.DeliveredAt);
                    string area = o.DeliveryLocation?.Split(',').FirstOrDefault()?.Trim() ?? "Unknown";
                    return new RecentTransactionDto
                    {
                        Customer = o.Customer?.Name ?? "Customer",
                        Area = area,
                        TimeAgo = timeAgo,
                        Amount = $"+EGP {DeliveryFeePerOrder:0}"
                    };
                }).ToList();

            // Next payout: total unpaid earnings (orders since last Monday)
            var nextPayoutDate = weekStart.AddDays(7).ToString("dddd, MMMM d, yyyy");

            return Result<CourierEarningsResponseDto>.Success(new CourierEarningsResponseDto
            {
                TotalEarned = weekEarnings,
                OrdersDone = ordersDoneThisWeek,
                HoursActive = 0, // No timesheet table — can be added later
                AvgPerOrder = Math.Round(avgPerOrder, 1),
                Cancelled = cancelled,
                CompletionRate = Math.Round(completionRate, 1),
                Rating = 4.8, // No ratings table for couriers yet
                TopPercent = 10, // Relative ranking — needs aggregation query
                DailyEarnings = dailyEarnings,
                RecentTransactions = recentTransactions,
                NextPayoutAmount = weekEarnings,
                NextPayoutDate = nextPayoutDate
            });
        }

        private static string FormatTimeAgo(DateTime? dt)
        {
            if (dt == null) return "Unknown";
            var diff = DateTime.UtcNow - dt.Value;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
            if (diff.TotalDays < 2) return "Yesterday";
            return $"{(int)diff.TotalDays} days ago";
        }
    }
}
