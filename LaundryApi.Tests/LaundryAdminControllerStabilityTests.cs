using LaundryApi.Controllers;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
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
using System.Security.Claims;

namespace LaundryApi.Tests;

public class LaundryAdminControllerStabilityTests
{
    [Fact]
    public async Task GetProfile_ReturnsOk_WhenLaundryExists()
    {
        using var context = BuildDbContext();
        await SeedLaundryAsync(context, "admin-1");

        var controller = BuildController(context, "admin-1");
        var result = await controller.GetProfile();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetServices_ReturnsOk_WhenLaundryExists()
    {
        using var context = BuildDbContext();
        await SeedLaundryAsync(context, "admin-1");
        await context.Services.AddAsync(new Service
        {
            LaundryId = 1,
            ServiceName = "Wash by Kilo",
            Category = ServiceCategory.Wash,
            Price = 50,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, "admin-1");
        var result = await controller.GetServices();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Dashboard_ReturnsOk_WhenNoLaundry()
    {
        using var context = BuildDbContext();
        var controller = BuildController(context, "admin-1");

        var result = await controller.GetDashboard();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddUpdateDeleteService_Flow_Works()
    {
        using var context = BuildDbContext();
        await SeedLaundryAsync(context, "admin-1");
        var controller = BuildController(context, "admin-1");

        var add = await controller.AddService(new AddServiceRequest
        {
            ServiceName = "Express Service",
            Category = ServiceCategory.Specialty,
            Price = 80,
            IsAvailable = true
        });
        Assert.IsType<OkObjectResult>(add);

        var created = await context.Services.FirstAsync(s => s.ServiceName == "Express Service");

        var update = await controller.UpdateService(created.Id, new UpdateServiceRequest
        {
            Price = 95,
            IsAvailable = false
        });
        Assert.IsType<OkObjectResult>(update);

        var delete = await controller.DeleteService(created.Id);
        Assert.IsType<OkObjectResult>(delete);
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

    private static async Task SeedLaundryAsync(NadeefDbContext context, string adminId)
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
            Id = 1,
            Name = "Laundry",
            Address = "Address",
            Latitude = 30,
            Longitude = 31,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = adminId,
            CreatedAt = DateTime.UtcNow
        });

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
