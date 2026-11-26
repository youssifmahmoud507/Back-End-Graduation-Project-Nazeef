using LaundryApi.Controllers;
using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.LaundryAdmin;
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
using System.Security.Claims;

namespace LaundryApi.Tests;

public class LaundryAdminCourierTests
{
    [Fact]
    public async Task AssignCourier_ReturnsNotFound_WhenCourierMissing()
    {
        using var context = BuildDbContext();
        var controller = BuildController(context, "admin-1");

        await SeedLaundryAsync(context, "admin-1");

        var result = await controller.AssignCourier(new AssignCourierRequest { PhoneNumber = "01000000000" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignCourier_AssignsCourier_WhenCourierExists()
    {
        using var context = BuildDbContext();
        var controller = BuildController(context, "admin-1");

        await SeedLaundryAsync(context, "admin-1");
        await SeedCourierAsync(context, "courier-1", "01011112222");

        var result = await controller.AssignCourier(new AssignCourierRequest { PhoneNumber = "01011112222" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = await context.CourierProfiles.FirstOrDefaultAsync(cp => cp.CourierId == "courier-1");

        Assert.NotNull(profile);
        Assert.NotNull(profile!.LaundryId);
        Assert.Equal(1, profile.LaundryId);
        Assert.NotNull(ok.Value);
    }

    private static LaundryAdminController BuildController(NadeefDbContext context, string adminId)
    {
        var controller = new LaundryAdminController(
            new FakeLaundryAdminService(),
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

    private static async Task SeedLaundryAsync(NadeefDbContext context, string adminId)
    {
        var laundry = new Laundry
        {
            Id = 1,
            Name = "Test Laundry",
            Address = "Test Address",
            Latitude = 30.1m,
            Longitude = 31.2m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };

        await context.Laundries.AddAsync(laundry);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCourierAsync(NadeefDbContext context, string courierId, string phone)
    {
        var courier = new ApplicationUser
        {
            Id = courierId,
            Name = "Courier",
            PhoneNumber = phone,
            Role = UserRole.Courier,
            IsActive = true
        };

        await context.Users.AddAsync(courier);
        await context.SaveChangesAsync();
    }

    private sealed class FakeLaundryAdminService : ILaundryAdminService
    {
        public Task<Result<bool>> AddServiceAsync(string adminId, AddServiceRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> DeleteServiceAsync(int serviceId, string adminId) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<LaundryProfileDto>> GetLaundryProfileAsync(string adminId) => Task.FromResult(Result<LaundryProfileDto>.Failure("Not implemented"));
        public Task<Result<List<LaundryServiceDto>>> GetLaundryServicesAsync(string adminId) => Task.FromResult(Result<List<LaundryServiceDto>>.Failure("Not implemented"));
        public Task<Result<List<LaundryOrderDto>>> GetIncomingOrdersAsync(string adminId) => Task.FromResult(Result<List<LaundryOrderDto>>.Failure("Not implemented"));
        public Task<Result<LaundryOrderDto>> GetOrderByIdAsync(int orderId, string adminId) => Task.FromResult(Result<LaundryOrderDto>.Failure("Not implemented"));
        public Task<Result<bool>> SetupLaundryProfileAsync(string adminId, SetupLaundryRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> UpdateLaundryProfileAsync(string adminId, SetupLaundryRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> UpdateAvailabilityAsync(string adminId, UpdateLaundryAvailabilityRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> UpdateOrderStatusAsync(int orderId, string adminId, UpdateOrderStatusRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> UpdateServiceAsync(int serviceId, string adminId, UpdateServiceRequest request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<string>> UploadLaundryImageAsync(string adminId, IFormFile file) => Task.FromResult(Result<string>.Failure("Not implemented"));
        public Task<Result<bool>> AssignCourierToOrderExplicitlyAsync(string adminId, LaundryShared.DTO.LaundryAdmin.AssignCourierRequestDto request) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<List<LaundryShared.DTO.LaundryAdmin.AvailableCourierDto>>> SuggestOrderToAvailableCouriersAsync(string adminId, LaundryShared.DTO.LaundryAdmin.BroadcastOrderRequestDto request) => Task.FromResult(Result<List<LaundryShared.DTO.LaundryAdmin.AvailableCourierDto>>.Failure("Not implemented"));
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
