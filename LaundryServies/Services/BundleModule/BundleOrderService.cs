using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LaundryDomain.IRepositories;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Models.Wallet;
using LaundryDomain.Specifications.BundleSpecs;
using LaundryDomain.Specifications.LaundryAdminSpec;
using LaundryDomain.Specifications.LaundrySpecsandwalletSpecs;
using LaundryDomain.Specifications.UserSpecs;
using LaundryServiesAbstraction.IServices.BundleModule;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.BundleModule;
using LaundryShared.DTO.Order;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums.WalletEnums;

// Use Wallet.CommissionLog — this is the entity registered in NadeefDbContext
using CommissionLog = LaundryDomain.Models.Wallet.CommissionLog;
using Order = LaundryDomain.Models.OrderModules.Order;
using OrderItem = LaundryDomain.Models.OrderModules.OrderItem;
//using Payment = LaundryDomain.Models.OrderModules.Payment;

namespace LaundryServies.Services.BundleModule
{
    public class BundleOrderService : IBundleOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BundleOrderService> _logger;

        // 100 points = 1 EGP
        private const decimal PointsConversionRate = 100m;

        public BundleOrderService(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            INotificationService notificationService,
            ILogger<BundleOrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<OrderDto>> PlaceBundleOrderAsync(
            PlaceBundleOrderRequest request, string userId)
        {
            try
            {
                // ── STEP 1: Validate Bundle ──────────────────────────────
                var bundleSpec = new BundleByIdWithItemsSpec(request.BundleId);
                var bundle = await _unitOfWork
                    .Repository<Bundle, Guid>()
                    .GetEntityWithSpecAsync(bundleSpec);

                if (bundle == null)
                    return Result<OrderDto>.Failure("Bundle not found");

                if (bundle.Status != BundleStatus.Active)
                    return Result<OrderDto>.Failure(
                        "This bundle is not currently available");

                if (bundle.ExpiryDate != null && bundle.ExpiryDate < DateTime.UtcNow)
                    return Result<OrderDto>.Failure("This bundle has expired");

                if (bundle.BundleItems == null || !bundle.BundleItems.Any())
                    return Result<OrderDto>.Failure("This bundle contains no items and cannot be ordered.");

                // ── STEP 2: Validate Bundle-Laundry Eligibility ──────────
                if (bundle.LaundryId.HasValue)
                {
                    if (bundle.LaundryId.Value != request.LaundryId)
                        return Result<OrderDto>.Failure(
                            "This bundle is not available at the selected laundry");
                }

                // ── STEP 3: Validate Customer Item Selections ────────────
                if (request.SelectedItems == null || !request.SelectedItems.Any())
                    return Result<OrderDto>.Failure("No items selected for this order");

                var servicesSpec = new ServicesByLaundryIdSpec(request.LaundryId);
                var laundryServices = await _unitOfWork
                    .Repository<Service, int>()
                    .GetAllWithSpecAsync(servicesSpec);

                int totalBundleQuantity = bundle.BundleItems.Sum(bi => bi.Quantity);
                int totalSelectedQuantity = request.SelectedItems.Sum(x => x.Quantity);

                if (totalSelectedQuantity != totalBundleQuantity)
                    return Result<OrderDto>.Failure(
                        "Item selection does not match bundle requirements");

                decimal bundleBasePrice = bundle.BundlePrice;
                decimal unitPrice = totalSelectedQuantity > 0
                    ? Math.Round(bundleBasePrice / totalSelectedQuantity, 2)
                    : 0;

                var orderItems = new List<OrderItem>();

                foreach (var item in request.SelectedItems)
                {
                    var bundleItem = bundle.BundleItems
                        .FirstOrDefault(bi => bi.Id == item.BundleItemId);

                    if (bundleItem == null)
                        return Result<OrderDto>.Failure(
                            "Invalid bundle item selection");

                    // For platform bundles: match by category, not exact service ID
                    // For laundry-specific bundles: match by exact service ID
                    LaundryDomain.Models.LaundryModules.Service? service;
                    if (!bundle.LaundryId.HasValue)
                    {
                        // Platform bundle — any laundry service in the same category is OK
                        service = laundryServices
                            .FirstOrDefault(s =>
                                bundleItem.AllowedItemCategory == null ||
                                s.Category == bundleItem.AllowedItemCategory);

                        if (service == null)
                            return Result<OrderDto>.Failure(
                                "No matching service category available at this laundry");
                    }
                    else
                    {
                        // Laundry-specific bundle — must match exact service ID
                        service = laundryServices
                            .FirstOrDefault(s => s.Id == item.ServiceId);

                        if (service == null)
                            return Result<OrderDto>.Failure(
                                "Service not available at this laundry");

                        if (bundleItem.AllowedItemCategory != null
                            && service.Category != bundleItem.AllowedItemCategory)
                            return Result<OrderDto>.Failure(
                                "Selected service does not match bundle item category");
                    }

                    orderItems.Add(new OrderItem
                    {
                        ServiceId = service.Id, // Save resolved service.Id!
                        Quantity = item.Quantity,
                        BundleItemId = item.BundleItemId,
                        UnitPrice = unitPrice,
                        Subtotal = unitPrice * item.Quantity
                    });
                }

                decimal customerServiceFee = Math.Round(bundleBasePrice * FinancialConstants.ServiceFeeRate, 2);
                decimal subtotal = bundleBasePrice + customerServiceFee + FinancialConstants.DeliveryFee;

                decimal pointsDiscount = 0m;
                UserPoints? userPoints = null;

                if (request.RedeemPoints != null && request.RedeemPoints > 0)
                {
                    var pointsSpec = new UserPointsByUserSpec(userId);
                    userPoints = await _unitOfWork
                        .Repository<UserPoints, int>()
                        .GetEntityWithSpecAsync(pointsSpec);

                    // UserPoints uses TotalPoints (not Balance)
                    if (userPoints == null
                        || userPoints.TotalPoints < request.RedeemPoints)
                        return Result<OrderDto>.Failure("Insufficient loyalty points");

                    pointsDiscount = ConvertPointsToEgp(request.RedeemPoints.Value);
                    subtotal -= pointsDiscount;
                    if (subtotal < 0) subtotal = 0;
                }

                decimal walletContribution = 0m;
                CustomerWallet? customerWallet = null;

                if (request.UseWalletBalance)
                {
                    var walletSpec = new CustomerWalletByCustomerIdSpec(userId);
                    customerWallet = await _unitOfWork
                        .Repository<CustomerWallet, int>()
                        .GetEntityWithSpecAsync(walletSpec);

                    if (customerWallet != null)
                        walletContribution = Math.Min(customerWallet.Balance, subtotal);

                    subtotal -= walletContribution;
                    if (subtotal < 0) subtotal = 0;
                }

                decimal finalAmount = subtotal;
                decimal laundryCommission = Math.Round(bundleBasePrice * 0.13m, 2);

                string pickupLoc = "Bundle Order";
                string deliveryLoc = "Bundle Order";



                if (!string.IsNullOrWhiteSpace(request.PickupLocation))
                    pickupLoc = request.PickupLocation;
                if (!string.IsNullOrWhiteSpace(request.DeliveryLocation))
                    deliveryLoc = request.DeliveryLocation;

                var order = new Order
                {
                    CustomerId = userId,
                    LaundryId = request.LaundryId,
                    PickupLocation = pickupLoc,
                    DeliveryLocation = deliveryLoc,
                    PickupTime = request.ScheduledPickupTime ?? DateTime.UtcNow,
                    DeliveryTime = request.ScheduledDeliveryTime
                                   ?? request.ScheduledPickupTime?.AddDays(1)
                                   ?? DateTime.UtcNow.AddDays(1),
                    IsBundleOrder = true,
                    Status = OrderStatus.PendingConfirmation,
                    TotalPrice = finalAmount,
                    DiscountAmount = pointsDiscount,
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                var orderPayment = new LaundryDomain.Models.OrderModules.Payment
                {
                    Amount = finalAmount,
                    PaymentStatus = PaymentStatus.Pending,
                    PaymentMethod = finalAmount == 0
                        ? PaymentMethod.Wallet
                        : PaymentMethod.CreditCard
                };
                order.Payment = orderPayment;

                await _unitOfWork.Repository<Order, int>().AddAsync(order);

                // First save to get Order.Id (int)
                await _unitOfWork.CompleteAsync();

                // ── Create BundleOrderMetadata (OrderId is now int) ──────
                var metadata = new BundleOrderMetadata
                {
                    Id = Guid.NewGuid(),          // Explicitly assign Guid ID!
                    OrderId = order.Id,           // int
                    BundleId = bundle.Id,          // Guid
                    BundleNameSnapshot = bundle.Name,
                    BundlePriceSnapshot = bundle.BundlePrice,
                    OriginalPriceSnapshot = bundle.OriginalPrice,
                    SavingsAmount = bundle.OriginalPrice - bundle.BundlePrice,
                    PurchasedAt = DateTime.UtcNow
                };

                try
                {
                    await _unitOfWork
                        .Repository<BundleOrderMetadata, Guid>()
                        .AddAsync(metadata);

                    order.BundleOrderMetadataId = metadata.Id;
                    _unitOfWork.Repository<Order, int>().Update(order);

                    // ── CommissionLog (Wallet.CommissionLog — registered in DbContext) ──
                    var commissionLog = new CommissionLog
                    {
                        OrderId = order.Id,
                        LaundryId = request.LaundryId,
                        CommissionAmount = laundryCommission,
                        OrderAmount = bundleBasePrice,
                        CommissionPercentage = 13m
                    };
                    await _unitOfWork
                        .Repository<CommissionLog, int>()
                        .AddAsync(commissionLog);

                    // ── Points Deduction ─────────────────────────────────────
                    if (request.RedeemPoints > 0 && userPoints != null)
                    {
                        var deductTx = new PointTransaction
                        {
                            UserPointsId = userPoints.Id,
                            Points = -request.RedeemPoints!.Value,
                            Type = PointTransactionType.Redeemed,
                            OrderId = order.Id,
                            Description = "Points redeemed for bundle order.",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork
                            .Repository<PointTransaction, int>()
                            .AddAsync(deductTx);

                        // TotalPoints is the correct property name
                        userPoints.TotalPoints -= request.RedeemPoints.Value;
                        _unitOfWork.Repository<UserPoints, int>().Update(userPoints);
                    }

                    // ── Points Earn ──────────────────────────────────────────
                    int earnedPoints = CalculateEarnedPoints(bundleBasePrice);
                    if (earnedPoints > 0)
                    {
                        if (userPoints == null)
                        {
                            var pointsSpec = new UserPointsByUserSpec(userId);
                            userPoints = await _unitOfWork
                                .Repository<UserPoints, int>()
                                .GetEntityWithSpecAsync(pointsSpec);
                        }

                        if (userPoints != null)
                        {
                            var earnTx = new PointTransaction
                            {
                                UserPointsId = userPoints.Id,
                                Points = earnedPoints,
                                Type = PointTransactionType.Earned,
                                OrderId = order.Id,
                                Description = "Points earned from bundle order.",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork
                                .Repository<PointTransaction, int>()
                                .AddAsync(earnTx);

                            // TotalPoints only — no TotalEarned property exists
                            userPoints.TotalPoints += earnedPoints;
                            _unitOfWork.Repository<UserPoints, int>().Update(userPoints);
                        }
                    }

                    // ── Wallet Deduction ─────────────────────────────────────
                    if (request.UseWalletBalance
                        && walletContribution > 0
                        && customerWallet != null)
                    {
                        var walletTx = new WalletTransaction
                        {
                            CustomerWalletId = customerWallet.Id,
                            Amount = walletContribution,
                            Type = TransactionType.Debit,
                            // Correct enum value from WalletEnums
                            Source = TransactionSource.WalletPayment,
                            OrderId = order.Id,
                            Status = TransactionStatus.Completed
                        };
                        await _unitOfWork
                            .Repository<WalletTransaction, int>()
                            .AddAsync(walletTx);

                        customerWallet.Balance -= walletContribution;
                        _unitOfWork.Repository<CustomerWallet, int>()
                            .Update(customerWallet);
                    }

                    // Final save for all above changes
                    await _unitOfWork.CompleteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save secondary bundle order details (metadata, commission, points, or wallet) for Order {OrderId}", order.Id);
                }

                // ── STEP 7: Notifications ────────────────────────────────
                try
                {
                    await _notificationService.SendNotificationAsync(
                        userId,
                        "Bundle Order Placed",
                        $"Your bundle order has been placed! " +
                        $"You saved EGP {metadata.SavingsAmount} with {bundle.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification for bundle order {OrderId}", order.Id);
                }

                // ── STEP 8: Return ───────────────────────────────────────
                var orderDto = MapToOrderDto(order, metadata);
                return Result<OrderDto>.Success(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error placing bundle order for User {UserId}", userId);
                return Result<OrderDto>.Failure(
                    "An error occurred while placing your bundle order.");
            }
        }

        public async Task<BundleOrderMetadataDto?> GetBundleMetadataByOrderAsync(
            int orderId)
        {
            var spec = new BundleOrderMetadataByOrderSpec(orderId);
            var metadata = await _unitOfWork
                .Repository<BundleOrderMetadata, Guid>()
                .GetEntityWithSpecAsync(spec);

            if (metadata == null) return null;

            return MapToMetadataDto(metadata);
        }

        // ── Private Helpers ──────────────────────────────────────────────

        private static decimal ConvertPointsToEgp(int points)
            => Math.Round(points / 100m, 2);

        private static int CalculateEarnedPoints(decimal amount)
            => (int)Math.Floor(amount);

        private static BundleOrderMetadataDto MapToMetadataDto(
            BundleOrderMetadata metadata)
        {
            return new BundleOrderMetadataDto
            {
                // Match exact property names in BundleOrderMetadataDto
                BundleId = metadata.BundleId,
                BundleName = metadata.BundleNameSnapshot,
                BundlePrice = metadata.BundlePriceSnapshot,
                OriginalPrice = metadata.OriginalPriceSnapshot,
                SavingsAmount = metadata.SavingsAmount,
                PurchasedAt = metadata.PurchasedAt
            };
        }

        private static OrderDto MapToOrderDto(
            Order order, BundleOrderMetadata? metadata = null)
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
                LaundryName = order.Laundry?.Name,
                Items = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    ServiceId = oi.ServiceId,
                    ServiceName = oi.Service?.ServiceName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal
                }).ToList() ?? new List<OrderItemDto>(),
                Payment = order.Payment == null ? null! : new PaymentDto
                {
                    Id = order.Payment.Id,
                    Amount = order.Payment.Amount,
                    PaymentStatus = order.Payment.PaymentStatus,
                    PaymentMethod = order.Payment.PaymentMethod,
                    PaymentDate = order.Payment.PaymentDate
                },
                // Add bundle metadata to order response
                BundleOrderMetadata = metadata == null
                    ? null
                    : MapToMetadataDto(metadata)
            };
        }
    }
}