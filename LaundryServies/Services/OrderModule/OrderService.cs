using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.LaundrySpecs;
using LaundryDomain.Specifications.OrderSpecs;
using LaundryDomain.Specifications.UserSpecs;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Order;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.Order;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.OrderModule
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKashierService _kashierService;
        private readonly INotificationService _notificationService;

        public OrderService(
            IUnitOfWork unitOfWork,
            IKashierService kashierService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _kashierService = kashierService;
            _notificationService = notificationService;
        }

        // POST /api/orders/calculate-price
        public async Task<Result<CalculatePriceResponse>> CalculatePriceAsync(CalculatePriceRequest request)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetByIdAsync(request.LaundryId);

            if (laundry == null)
                return Result<CalculatePriceResponse>.Failure("Laundry not found.");

            var items = new List<OrderItemPriceDto>();
            decimal totalPrice = 0;

            foreach (var item in request.Items)
            {
                var service = await _unitOfWork.Repository<Service, int>()
                                               .GetByIdAsync(item.ServiceId);

                if (service == null)
                    return Result<CalculatePriceResponse>.Failure($"Service {item.ServiceId} not found.");

                if (!service.IsAvailable)
                    return Result<CalculatePriceResponse>.Failure($"Service {service.ServiceName} is not available.");

                var subtotal = service.Price * item.Quantity;
                totalPrice += subtotal;

                items.Add(new OrderItemPriceDto
                {
                    ServiceId = service.Id,
                    ServiceName = service.ServiceName,
                    Quantity = item.Quantity,
                    UnitPrice = service.Price,
                    Subtotal = subtotal
                });
            }

            var (coupon, discountAmount, couponError) = await ValidateCouponAsync(
                request.CouponCode,
                request.LaundryId,
                totalPrice,
                null,
                false);

            if (!string.IsNullOrWhiteSpace(couponError))
                return Result<CalculatePriceResponse>.Failure(couponError);

            var finalPrice = Math.Max(0, totalPrice - discountAmount);

            var response = new CalculatePriceResponse
            {
                TotalPrice = totalPrice,
                DiscountAmount = discountAmount,
                FinalPrice = finalPrice,
                Items = items
            };

            return Result<CalculatePriceResponse>.Success(response);
        }
        // POST /api/orders
        public async Task<Result<OrderDto>> PlaceOrderAsync(string customerId, PlaceOrderRequest request)
        {
            // 1. Check Laundry exists and available
            var laundrySpec = new LaundryWithServicesSpec(request.LaundryId);
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetEntityWithSpecAsync(laundrySpec);

            if (laundry == null)
                return Result<OrderDto>.Failure("Laundry not found.");

            if (laundry.Status != LaundryStatus.Active)
                return Result<OrderDto>.Failure("Laundry is not active.");

            if (laundry.Availability != LaundryAvailability.Open)
                return Result<OrderDto>.Failure("Laundry is currently unavailable.");

            // 2. Build OrderItems + Calculate Total
            var orderItems = new List<OrderItem>();
            decimal totalPrice = 0;

            foreach (var item in request.Items)
            {
                var service = await _unitOfWork.Repository<Service, int>()
                                               .GetByIdAsync(item.ServiceId);

                if (service == null)
                    return Result<OrderDto>.Failure($"Service {item.ServiceId} not found.");

                if (!service.IsAvailable)
                    return Result<OrderDto>.Failure($"Service {service.ServiceName} is not available.");

                var subtotal = service.Price * item.Quantity;
                totalPrice += subtotal;

                orderItems.Add(new OrderItem
                {
                    ServiceId = item.ServiceId,
                    Quantity = item.Quantity,
                    UnitPrice = service.Price,
                    Subtotal = subtotal
                });
            }

            var (coupon, discountAmount, couponError) = await ValidateCouponAsync(
                request.CouponCode,
                request.LaundryId,
                totalPrice,
                customerId,
                true);

            if (!string.IsNullOrWhiteSpace(couponError))
                return Result<OrderDto>.Failure(couponError);

            var finalPrice = Math.Max(0, totalPrice - discountAmount);

            // 3. Create Order
            var order = new Order
            {
                CustomerId = customerId,
                LaundryId = request.LaundryId,
                PickupLocation = request.PickupLocation,
                DeliveryLocation = request.DeliveryLocation,
                PickupTime = request.PickupTime,
                DeliveryTime = request.DeliveryTime,
                Notes = request.Notes,
                Status = OrderStatus.PendingConfirmation,
                TotalPrice = finalPrice,
                DiscountAmount = discountAmount,
                CouponId = coupon?.Id,
                OrderItems = orderItems
            };

            // 4. Create Payment (Pending)
            var payment = new LaundryDomain.Models.OrderModules.Payment
            {
                Amount = finalPrice * (1 + FinancialConstants.ServiceFeeRate) + FinancialConstants.DeliveryFee,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.Cash
            };

            order.Payment = payment;

            if (coupon != null)
            {
                var userCoupon = new UserCoupon
                {
                    UserId = customerId,
                    CouponId = coupon.Id
                };

                coupon.CurrentUsage += 1;
                await _unitOfWork.Repository<UserCoupon, int>().AddAsync(userCoupon);
                _unitOfWork.Repository<Coupon, int>().Update(coupon);
            }

            // 5. Save
            await _unitOfWork.Repository<Order, int>().AddAsync(order);
            await _unitOfWork.CompleteAsync();

            // 6. Send Notification to Laundry Admin
            try
            {
                if (!string.IsNullOrEmpty(laundry.AdminId))
                {
                    await _notificationService.SendNotificationAsync(
                        laundry.AdminId,
                        "New Order Received! 🛒",
                        $"A new order #{order.Id} has been placed. Tap to view and accept."
                    );
                }
            }
            catch (Exception)
            {
                // Non-blocking fallback to ensure order placement succeeds even if notification hub is offline.
            }

            return Result<OrderDto>.Success(MapToOrderDto(order));
        }
        // GET /api/orders
        public async Task<Result<Pagination<OrderDto>>> GetCustomerOrdersAsync(string customerId, OrderParams orderParams)
        {
            var spec = new OrdersByCustomerSpec(customerId, orderParams);
            var countSpec = new OrdersByCustomerCountSpec(customerId, orderParams);

            var orders = await _unitOfWork.Repository<Order, int>()
                                          .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Order, int>()
                                         .CountAsync(countSpec);

            var data = orders.Select(o => MapToOrderDto(o)).ToList();

            var pagination = new Pagination<OrderDto>(
                orderParams.PageIndex,
                orderParams.PageSize,
                count,
                data
            );

            return Result<Pagination<OrderDto>>.Success(pagination);
        }
        // GET /api/orders/active
        public async Task<Result<List<OrderDto>>> GetActiveOrdersAsync(string customerId)
        {
            var spec = new ActiveOrdersByCustomerSpec(customerId);

            var orders = await _unitOfWork.Repository<Order, int>()
                                          .GetAllWithSpecAsync(spec);

            var data = orders.Select(o => MapToOrderDto(o)).ToList();

            return Result<List<OrderDto>>.Success(data);
        }
        // GET /api/orders/history
        public async Task<Result<Pagination<OrderDto>>> GetOrderHistoryAsync(string customerId, OrderParams orderParams)
        {
            var spec = new OrderHistorySpec(customerId, orderParams);
            var countSpec = new OrderHistoryCountSpec(customerId);

            var orders = await _unitOfWork.Repository<Order, int>()
                                          .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Order, int>()
                                         .CountAsync(countSpec);

            var data = orders.Select(o => MapToOrderDto(o)).ToList();

            var pagination = new Pagination<OrderDto>(
                orderParams.PageIndex,
                orderParams.PageSize,
                count,
                data
            );

            return Result<Pagination<OrderDto>>.Success(pagination);
        }
        // GET /api/orders/{id}
        public async Task<Result<OrderDto>> GetOrderByIdAsync(string customerId, int orderId)
        {
            var spec = new OrderWithDetailsSpec(orderId, customerId);

            var order = await _unitOfWork.Repository<Order, int>()
                                         .GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<OrderDto>.Failure("Order not found.");

            return Result<OrderDto>.Success(MapToOrderDto(order));
        }
        // GET /api/orders/{id}/track
        public async Task<Result<OrderTrackDto>> TrackOrderAsync(string customerId, int orderId)
        {
            var spec = new OrderWithDetailsSpec(orderId, customerId);

            var order = await _unitOfWork.Repository<Order, int>()
                                         .GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<OrderTrackDto>.Failure("Order not found.");

            var trackDto = new OrderTrackDto
            {
                OrderId = order.Id,
                Status = order.Status,
                LaundryName = order.Laundry.Name,
                LaundryAddress = order.Laundry.Address,
                PickupTime = order.PickupTime,
                DeliveryTime = order.DeliveryTime,
                EstimatedDeliveryTime = order.EstimatedDeliveryTime,
                CourierLatitude = (decimal?)order.CourierLatitude,
                CourierLongitude = (decimal?)order.CourierLongitude,
                CourierId = order.CourierId,
                CourierName = order.Courier?.Name,
                CourierPhone = order.Courier?.PhoneNumber,
                CourierAvatar = order.Courier?.AvatarUrl
            };

            return Result<OrderTrackDto>.Success(trackDto);
        }
        // PUT /api/orders/{id}/cancel
        public async Task<Result<bool>> CancelOrderAsync(string customerId, int orderId, CancelOrderRequest request)
        {
            var spec = new OrderWithDetailsSpec(orderId, customerId);

            var order = await _unitOfWork.Repository<Order, int>()
                                         .GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<bool>.Failure("Order not found.");

            // Allow cancellation only if the order is still pending confirmation or just accepted and not yet picked up
            if (order.Status != OrderStatus.PendingConfirmation && order.Status != OrderStatus.Accepted)
                return Result<bool>.Failure(order.Status == OrderStatus.Cancelled 
                    ? "Order is already cancelled." 
                    : "Orders can only be cancelled while pending confirmation or waiting for courier.");

            if (order.Payment != null && order.Payment.PaymentStatus == PaymentStatus.Paid && order.Payment.PaymentMethod != PaymentMethod.Cash)
            {
                // الأوردر مدفوع (بطاقة أو محفظة أو مزيج) — استرجاع كامل المبلغ للمحفظة
                var refundResult = await _kashierService.RefundWalletPaymentAsync(order.CustomerId, order.Id, order.Payment.Amount);
                if (!refundResult.IsSuccess)
                {
                    return Result<bool>.Failure("Error refunding payment: " + refundResult.ErrorMessage);
                }
            }
            else if (order.Payment != null && order.Payment.PaymentStatus != PaymentStatus.Paid)
            {
                // الأوردر لم يُدفع بعد بالبطاقة لكن ربما تم خصم من المحفظة — استرجاع أي خصم محفظة
                var walletDebitSpec = new LaundryDomain.Specifications.BaseSpecification<LaundryDomain.Models.Wallet.WalletTransaction, int>(
                    t => t.OrderId == orderId &&
                         t.Type == LaundryShared.Enums.WalletEnums.TransactionType.Debit &&
                         t.Source == LaundryShared.Enums.WalletEnums.TransactionSource.WalletPayment &&
                         t.Status == LaundryShared.Enums.WalletEnums.TransactionStatus.Completed);

                var walletDebitTx = await _unitOfWork.Repository<LaundryDomain.Models.Wallet.WalletTransaction, int>()
                    .GetEntityWithSpecAsync(walletDebitSpec);

                if (walletDebitTx != null && walletDebitTx.CustomerWalletId.HasValue)
                {
                    var customerWallet = await _unitOfWork.Repository<LaundryDomain.Models.Wallet.CustomerWallet, int>()
                        .GetByIdAsync(walletDebitTx.CustomerWalletId.Value);

                    if (customerWallet != null)
                    {
                        customerWallet.Balance += walletDebitTx.Amount;

                        var refundTx = new LaundryDomain.Models.Wallet.WalletTransaction
                        {
                            CustomerWalletId = customerWallet.Id,
                            OrderId = orderId,
                            Amount = walletDebitTx.Amount,
                            Type = LaundryShared.Enums.WalletEnums.TransactionType.Credit,
                            Source = LaundryShared.Enums.WalletEnums.TransactionSource.Refund,
                            Status = LaundryShared.Enums.WalletEnums.TransactionStatus.Completed,
                            CreatedAt = DateTime.UtcNow
                        };

                        _unitOfWork.Repository<LaundryDomain.Models.Wallet.CustomerWallet, int>().Update(customerWallet);
                        await _unitOfWork.Repository<LaundryDomain.Models.Wallet.WalletTransaction, int>().AddAsync(refundTx);
                    }
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = request.CancellationReason;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            // Notify laundry admin about order cancellation
            try
            {
                if (order.Laundry != null && !string.IsNullOrEmpty(order.Laundry.AdminId))
                {
                    await _notificationService.SendNotificationAsync(
                        order.Laundry.AdminId,
                        "Order Cancelled ❌",
                        $"Order #{order.Id} has been cancelled by the customer. Reason: {request.CancellationReason}"
                    );
                }
                else
                {
                    var laundry = await _unitOfWork.Repository<LaundryDomain.Models.LaundryModules.Laundry, int>().GetByIdAsync(order.LaundryId);
                    if (laundry != null && !string.IsNullOrEmpty(laundry.AdminId))
                    {
                        await _notificationService.SendNotificationAsync(
                            laundry.AdminId,
                            "Order Cancelled ❌",
                            $"Order #{order.Id} has been cancelled by the customer. Reason: {request.CancellationReason}"
                        );
                    }
                }
            }
            catch (Exception)
            {
                // Non-blocking fallback
            }

            return Result<bool>.Success(true);
        }
        // GET /api/orders/{id}/payment
        public async Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId)
        {
            var spec = new OrderWithDetailsSpec(orderId, customerId);

            var order = await _unitOfWork.Repository<Order, int>()
                                         .GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<PaymentDto>.Failure("Order not found.");

            if (order.Payment == null)
                return Result<PaymentDto>.Failure("No payment found for this order.");

            var paymentDto = new PaymentDto
            {
                Id = order.Payment.Id,
                Amount = order.Payment.Amount,
                PaymentStatus = order.Payment.PaymentStatus,
                PaymentMethod = order.Payment.PaymentMethod,
                PaymentDate = order.Payment.PaymentDate
            };

            return Result<PaymentDto>.Success(paymentDto);
        }
        // Private Manual Mapping
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                DiscountAmount = order.DiscountAmount,
                PickupTime = order.PickupTime,
                DeliveryTime = order.DeliveryTime,
                PickupLocation = order.PickupLocation,
                DeliveryLocation = order.DeliveryLocation,
                Notes = order.Notes,
                CancellationReason = order.CancellationReason,
                CreatedAt = order.CreatedAt,
                LaundryId = order.LaundryId,
                LaundryName = order.Laundry?.Name,
                Items = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    ServiceId = oi.ServiceId,
                    ServiceName = oi.Service?.ServiceName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal
                }).ToList() ?? new List<OrderItemDto>(),
                Payment = order.Payment == null ? null : new PaymentDto
                {
                    Id = order.Payment.Id,
                    Amount = order.Payment.Amount,
                    PaymentStatus = order.Payment.PaymentStatus,
                    PaymentMethod = order.Payment.PaymentMethod,
                    PaymentDate = order.Payment.PaymentDate
                }
            };
        }

        private async Task<(Coupon? Coupon, decimal DiscountAmount, string? ErrorMessage)> ValidateCouponAsync(
            string? couponCode,
            int laundryId,
            decimal totalPrice,
            string? userId,
            bool checkUserUsage)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
                return (null, 0, null);

            var normalizedCode = couponCode.Trim();
            var coupon = await _unitOfWork.Repository<Coupon, int>()
                .GetEntityWithSpecAsync(new CouponByCodeSpec(normalizedCode));

            if (coupon == null || !coupon.IsActive)
                return (null, 0, "Invalid or inactive coupon.");

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
                return (null, 0, "Coupon expired.");

            if (coupon.LaundryId.HasValue && coupon.LaundryId.Value != laundryId)
                return (null, 0, "Coupon is not valid for this laundry.");

            if (coupon.UsageLimit.HasValue && coupon.CurrentUsage >= coupon.UsageLimit.Value)
                return (null, 0, "Coupon usage limit reached.");

            if (checkUserUsage && !string.IsNullOrWhiteSpace(userId))
            {
                var usedSpec = new UserCouponByUserAndCouponSpec(userId, coupon.Id);
                var existing = await _unitOfWork.Repository<UserCoupon, int>()
                    .GetEntityWithSpecAsync(usedSpec);

                if (existing != null)
                    return (null, 0, "Coupon already used by this user.");
            }

            var discountAmount = coupon.IsPercentage
                ? Math.Round(totalPrice * (coupon.DiscountValue / 100m), 2)
                : coupon.DiscountValue;

            discountAmount = Math.Min(discountAmount, totalPrice);

            return (coupon, discountAmount, null);
        }
    }
}
