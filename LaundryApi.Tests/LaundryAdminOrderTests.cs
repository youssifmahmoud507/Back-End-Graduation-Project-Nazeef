using LaundryApi.Controllers;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using Moq;
using LaundryPerstince.Data.Context;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Common;
using LaundryServies.Services.LaundryAdminService;
using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.AI;
using LaundryShared.DTO.LaundryAdmin;
using LaundryShared.DTO.Notifications;
using LaundryShared.DTO.PaymentKashier;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LaundryApi.Tests;

public class LaundryAdminOrderTests
{
    [Fact]
    public async Task GetOrderById_ReturnsOrderFromDatabase()
    {
        using var context = BuildDbContext();
        var adminId = "admin-1";
        var customerId = "cust-1";

        await SeedOrderAsync(context, adminId, customerId);

        var controller = BuildController(context, adminId);

        var result = await controller.GetOrderById(1001);

        var ok = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LaundryOrderDto>(ok.Value);

        Assert.Equal(1001, order.Id);
        Assert.Equal("???? ?????", order.CustomerName);
        Assert.Equal("01012345678", order.CustomerPhone);
        Assert.Equal("????? ??? - ???? ??????", order.PickupLocation);
        Assert.Equal("???????? - ???? ?????", order.DeliveryLocation);
        Assert.Equal(2, order.Items.Count);
        Assert.Contains(order.Items, i => i.ServiceName == "???? ????" && i.Quantity == 3);
        Assert.Contains(order.Items, i => i.ServiceName == "??????" && i.Quantity == 2);
    }

    private static LaundryAdminController BuildController(NadeefDbContext context, string adminId)
    {
        var unitOfWork = new UnitOfWork(context);
        var laundryAdminService = new LaundryAdminService(unitOfWork, new FakeFileService(), null!, null!, new FakeKashierService());

        var controller = new LaundryAdminController(
            laundryAdminService,
            new FakePriceRecommendationService(),
            new FakeNotificationService(),
            context,
            new FakeKashierService());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("uid", adminId),
            new Claim(ClaimTypes.Role, UserRole.LaundryAdmin.ToString())
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    private static NadeefDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NadeefDbContext(options);
    }

    private static async Task SeedOrderAsync(NadeefDbContext context, string adminId, string customerId)
    {
        var admin = new ApplicationUser
        {
            Id = adminId,
            Name = "???? ???????",
            PhoneNumber = "01000000000",
            Role = UserRole.LaundryAdmin,
            IsActive = true
        };

        var customer = new ApplicationUser
        {
            Id = customerId,
            Name = "???? ?????",
            PhoneNumber = "01012345678",
            Role = UserRole.Customer,
            IsActive = true
        };

        var laundry = new Laundry
        {
            Id = 5,
            Name = "????? ?????",
            Address = "????? ???",
            Latitude = 30.06m,
            Longitude = 31.34m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };

        var service1 = new Service
        {
            Id = 201,
            LaundryId = laundry.Id,
            ServiceName = "???? ????",
            Category = ServiceCategory.Wash,
            Price = 25m,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var service2 = new Service
        {
            Id = 202,
            LaundryId = laundry.Id,
            ServiceName = "??????",
            Category = ServiceCategory.Specialty,
            Price = 40m,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = 1001,
            CustomerId = customerId,
            LaundryId = laundry.Id,
            PickupLocation = "????? ??? - ???? ??????",
            DeliveryLocation = "???????? - ???? ?????",
            PickupTime = DateTime.UtcNow.AddHours(2),
            Status = OrderStatus.Accepted,
            TotalPrice = 155m,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Laundry = laundry,
            OrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    ServiceId = service1.Id,
                    Service = service1,
                    Quantity = 3,
                    UnitPrice = service1.Price,
                    Subtotal = service1.Price * 3
                },
                new OrderItem
                {
                    ServiceId = service2.Id,
                    Service = service2,
                    Quantity = 2,
                    UnitPrice = service2.Price,
                    Subtotal = service2.Price * 2
                }
            },
            Payment = new Payment
            {
                Amount = 155m,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.Cash,
                PaymentDate = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(admin, customer);
        await context.Laundries.AddAsync(laundry);
        await context.Services.AddRangeAsync(service1, service2);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    private sealed class FakeFileService : IFileService
    {
        public Task<string> UploadImageAsync(IFormFile file, string folder) => Task.FromResult("https://example.com/image.jpg");
        public Task<string> UploadPdfAsync(IFormFile file, string folder) => Task.FromResult("https://example.com/doc.pdf");
        public Task DeleteImageAsync(string publicId) => Task.CompletedTask;
    }

    private sealed class FakePriceRecommendationService : IPriceRecommendationService
    {
        public Task<PriceRecommendationDto?> GetPriceRecommendationAsync(string serviceName, ServiceCategory category)
            => Task.FromResult<PriceRecommendationDto?>(null);
    }

    private sealed class FakeKashierService : IKashierService
    {
        public Task<Result<KashierPaymentDto>> CreateWalletChargeAsync(string customerId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateMobileWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<bool>> CreateCashPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateCardPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateCommissionPaymentAsync(string laundryAdminId, int laundryId, decimal amount, string currency = "EGP") => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<bool>> HandleWebhookAsync(string signature, KashierWebhookDto webhook) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> ProcessPayoutAsync(int laundryWalletId) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> CreateWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> RefundWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> ProcessCallbackPaymentAsync(string merchantOrderId, decimal amount, string status) => Task.FromResult(Result<bool>.Failure("Not implemented"));
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public Task<Result<Pagination<NotificationDto>>> GetNotificationsAsync(string userId, NotificationParams notificationParams)
            => Task.FromResult(Result<Pagination<NotificationDto>>.Failure("Not implemented"));
        public Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(string userId)
            => Task.FromResult(Result<List<NotificationDto>>.Failure("Not implemented"));
        public Task<Result<int>> GetUnreadCountAsync(string userId)
            => Task.FromResult(Result<int>.Failure("Not implemented"));
        public Task<Result<bool>> MarkAsReadAsync(string userId, int notificationId)
            => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<int>> MarkAllAsReadAsync(string userId)
            => Task.FromResult(Result<int>.Failure("Not implemented"));
        public Task<Result<bool>> DeleteNotificationAsync(string userId, int notificationId)
            => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<int>> ClearAllReadAsync(string userId)
            => Task.FromResult(Result<int>.Failure("Not implemented"));
        public Task<Result<NotificationPreferenceDto>> GetPreferencesAsync(string userId)
            => Task.FromResult(Result<NotificationPreferenceDto>.Failure("Not implemented"));
        public Task<Result<NotificationPreferenceDto>> UpdatePreferencesAsync(string userId, UpdateNotificationPreferenceRequest request)
            => Task.FromResult(Result<NotificationPreferenceDto>.Failure("Not implemented"));
        public Task SendNotificationAsync(string userId, string title, string message) => Task.CompletedTask;
        public Task SendOrderConfirmedAsync(string customerId, int orderId) => Task.CompletedTask;
        public Task SendOrderInProgressAsync(string customerId, int orderId) => Task.CompletedTask;
        public Task SendOrderReadyAsync(string customerId, int orderId) => Task.CompletedTask;
        public Task SendOrderDeliveredAsync(string customerId, int orderId) => Task.CompletedTask;
        public Task SendOrderCancelledAsync(string customerId, int orderId, string reason) => Task.CompletedTask;
        public Task SendOrderBroadcastToCouriersAsync(List<string> courierUserIds, int orderId, string laundryName, string pickupArea, string dropArea) => Task.CompletedTask;
        public Task SendCourierAssignedToCustomerAsync(string customerId, int orderId, string courierName, string courierPhone, string eta) => Task.CompletedTask;
    }
}

public class LaundryAdminService_UpdateOrderStatus
{
    private static NadeefDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NadeefDbContext(options);
    }

    [Fact]
    public async Task CancelOrder_PaidOnline_RefundsToCustomerWallet()
    {
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);
        
        var kashierMock = new Mock<IKashierService>();
        kashierMock.Setup(k => k.RefundWalletPaymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()))
                   .ReturnsAsync(Result<bool>.Success(true));

        var adminId = "admin-1";
        var customerId = "cust-1";
        
        // Seed database
        var admin = new ApplicationUser { Id = adminId, Name = "Admin", Role = UserRole.LaundryAdmin, IsActive = true };
        var customer = new ApplicationUser { Id = customerId, Name = "Customer", Role = UserRole.Customer, IsActive = true };
        var laundry = new Laundry { Id = 5, Name = "Test Laundry", Address = "Cairo", AdminId = adminId, Status = LaundryStatus.Active };
        var order = new Order
        {
            Id = 1001,
            CustomerId = customerId,
            LaundryId = laundry.Id,
            Status = OrderStatus.Accepted,
            TotalPrice = 155m,
            PickupLocation = "Home",
            DeliveryLocation = "Home",
            Payment = new Payment
            {
                Amount = 155m,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.CreditCard
            }
        };

        await context.Users.AddRangeAsync(admin, customer);
        await context.Laundries.AddAsync(laundry);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var service = new LaundryAdminService(unitOfWork, new FakeFileService(), null!, null!, kashierMock.Object);

        var result = await service.UpdateOrderStatusAsync(1001, adminId, new UpdateOrderStatusRequest { NewStatus = OrderStatus.Cancelled, CancellationReason = "Reason" });

        Assert.True(result.IsSuccess);
        kashierMock.Verify(k => k.RefundWalletPaymentAsync(customerId, 1001, 155m), Times.Once);
    }

    [Fact]
    public async Task AssignCourierToOrderExplicitlyAsync_OrderStatusPendingConfirmation_TransitionsToAccepted()
    {
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var adminId = "admin-1";
        var courierId = "courier-1";
        var orderId = 2001;

        var admin = new ApplicationUser { Id = adminId, Name = "Admin", Role = UserRole.LaundryAdmin, IsActive = true };
        var courier = new ApplicationUser { Id = courierId, Name = "Courier", Role = UserRole.Courier, IsActive = true };
        var laundry = new Laundry { Id = 5, Name = "Test Laundry", Address = "Cairo", AdminId = adminId, Status = LaundryStatus.Active };
        var order = new Order
        {
            Id = orderId,
            CustomerId = "cust-1",
            LaundryId = laundry.Id,
            Status = OrderStatus.PendingConfirmation,
            TotalPrice = 155m,
            PickupLocation = "Home",
            DeliveryLocation = "Home"
        };

        await context.Users.AddRangeAsync(admin, courier);
        await context.Laundries.AddAsync(laundry);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByIdAsync(courierId))
                       .ReturnsAsync(courier);

        var notificationMock = new Mock<INotificationService>();
        var kashierMock = new Mock<IKashierService>();

        var service = new LaundryAdminService(unitOfWork, new FakeFileService(), notificationMock.Object, userManagerMock.Object, kashierMock.Object);

        var request = new AssignCourierRequestDto { OrderId = orderId, CourierId = courierId };

        var result = await service.AssignCourierToOrderExplicitlyAsync(adminId, request);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.FindAsync(orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(courierId, updatedOrder!.CourierId);
        Assert.Equal(OrderStatus.Accepted, updatedOrder.Status);
    }

    [Fact]
    public async Task AssignCourierToOrderExplicitlyAsync_OrderStatusWashing_PreservesWashingStatus()
    {
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var adminId = "admin-1";
        var courierId = "courier-1";
        var orderId = 2002;

        var admin = new ApplicationUser { Id = adminId, Name = "Admin", Role = UserRole.LaundryAdmin, IsActive = true };
        var courier = new ApplicationUser { Id = courierId, Name = "Courier", Role = UserRole.Courier, IsActive = true };
        var laundry = new Laundry { Id = 5, Name = "Test Laundry", Address = "Cairo", AdminId = adminId, Status = LaundryStatus.Active };
        var order = new Order
        {
            Id = orderId,
            CustomerId = "cust-1",
            LaundryId = laundry.Id,
            Status = OrderStatus.Washing,
            TotalPrice = 155m,
            PickupLocation = "Home",
            DeliveryLocation = "Home"
        };

        await context.Users.AddRangeAsync(admin, courier);
        await context.Laundries.AddAsync(laundry);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByIdAsync(courierId))
                       .ReturnsAsync(courier);

        var notificationMock = new Mock<INotificationService>();
        var kashierMock = new Mock<IKashierService>();

        var service = new LaundryAdminService(unitOfWork, new FakeFileService(), notificationMock.Object, userManagerMock.Object, kashierMock.Object);

        var request = new AssignCourierRequestDto { OrderId = orderId, CourierId = courierId };

        var result = await service.AssignCourierToOrderExplicitlyAsync(adminId, request);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.FindAsync(orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(courierId, updatedOrder!.CourierId);
        Assert.Equal(OrderStatus.Washing, updatedOrder.Status);
    }

    private sealed class FakeFileService : IFileService
    {
        public Task<string> UploadImageAsync(IFormFile file, string folder) => Task.FromResult("https://example.com/image.jpg");
        public Task<string> UploadPdfAsync(IFormFile file, string folder) => Task.FromResult("https://example.com/doc.pdf");
        public Task DeleteImageAsync(string publicId) => Task.CompletedTask;
    }
}
