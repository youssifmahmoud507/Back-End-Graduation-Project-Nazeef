using LaundryApi.Controllers;
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

public class LaundryAdminServiceCatalogTests
{
    [Fact]
    public async Task GetServiceCatalog_ReturnsEnglishCatalogAndMarksAddedService()
    {
        using var context = BuildDbContext();
        var adminId = "admin-1";
        await SeedLaundryWithServiceAsync(context, adminId);

        var controller = BuildController(context, adminId, new FakePriceRecommendationService());

        var result = await controller.GetServiceCatalog(false);

        var ok = Assert.IsType<OkObjectResult>(result);
        var catalog = Assert.IsType<List<ServiceCatalogItemDto>>(ok.Value);

        Assert.Contains(catalog, x => x.ServiceName == "قميص - Shirt" && x.Category == ServiceCategory.Iron);
        var dryClean = Assert.Single(catalog.Where(x => x.ServiceName == "قميص - Shirt" && x.Category == ServiceCategory.DryCleaning));
        Assert.True(dryClean.IsAdded);
        Assert.Equal(30m, dryClean.CurrentPrice);
        Assert.False(dryClean.IsAvailable);
    }

    [Fact]
    public async Task AddService_UsesRequestedAvailability()
    {
        using var context = BuildDbContext();
        var adminId = "admin-1";
        await SeedLaundryOnlyAsync(context, adminId);

        var controller = BuildController(context, adminId, new FakePriceRecommendationService());

        var response = await controller.AddService(new AddServiceRequest
        {
            ServiceName = "Express Service",
            Category = ServiceCategory.Specialty,
            Price = 85m,
            IsAvailable = false
        });

        Assert.IsType<OkObjectResult>(response);

        var saved = await context.Services.FirstOrDefaultAsync(s => s.ServiceName == "Express Service");
        Assert.NotNull(saved);
        Assert.False(saved!.IsAvailable);
    }

    private static LaundryAdminController BuildController(NadeefDbContext context, string adminId, IPriceRecommendationService priceService)
    {
        var controller = new LaundryAdminController(
            new FakeLaundryAdminService(),
            priceService,
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

    private static async Task SeedLaundryOnlyAsync(NadeefDbContext context, string adminId)
    {
        await context.Users.AddAsync(new ApplicationUser
        {
            Id = adminId,
            Name = "Admin",
            Role = UserRole.LaundryAdmin,
            IsActive = true
        });

        await context.Laundries.AddAsync(new Laundry
        {
            Id = 10,
            Name = "My Laundry",
            Address = "Address",
            Latitude = 30.1m,
            Longitude = 31.2m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedLaundryWithServiceAsync(NadeefDbContext context, string adminId)
    {
        await SeedLaundryOnlyAsync(context, adminId);

        await context.Services.AddAsync(new Service
        {
            Id = 1,
            LaundryId = 10,
            ServiceName = "قميص - Shirt",
            Category = ServiceCategory.DryCleaning,
            Price = 30m,
            IsAvailable = false,
            CreatedAt = DateTime.UtcNow
        });

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
            => Task.FromResult<PriceRecommendationDto?>(new PriceRecommendationDto
            {
                SuggestedMinPrice = 50,
                SuggestedMaxPrice = 100,
                RecommendedPrice = 75,
                Reasoning = "Average market range"
            });
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
