using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryServies.Services.Courier;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryShared.Common;
using LaundryShared.DTO.Courier;
using LaundryShared.DTO.Notifications;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using LaundryServiesAbstraction.IServices.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LaundryApi.Tests;

// ═══════════════════════════════════════════════════════════════════
//  Helpers shared across all courier tests
// ═══════════════════════════════════════════════════════════════════
file static class CourierTestHelpers
{
    public static NadeefDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NadeefDbContext(opts);
    }

    public static (CourierService svc, Mock<UserManager<ApplicationUser>> umMock)
        BuildService(NadeefDbContext ctx)
    {
        var uow       = new LaundryPerstince.Data.Repositories.UnitOfWork(ctx);
        var store     = new Mock<IUserStore<ApplicationUser>>();
        var umMock    = new Mock<UserManager<ApplicationUser>>(
                            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var notifMock = new Mock<INotificationService>();
        notifMock.Setup(n => n.SendOrderDeliveredAsync(It.IsAny<string>(), It.IsAny<int>()))
                 .Returns(Task.CompletedTask);
        var kashierMock = new Mock<IKashierService>();

        var svc = new CourierService(uow, umMock.Object, notifMock.Object, kashierMock.Object);
        return (svc, umMock);
    }

    // ── seed helpers ─────────────────────────────────────────────
    public static async Task<ApplicationUser> SeedCourierUserAsync(
        NadeefDbContext ctx, string id = "courier-1", string phone = "01000000001")
    {
        var user = new ApplicationUser
        {
            Id = id, Name = "Test Courier",
            PhoneNumber = phone, Role = UserRole.Courier, IsActive = true,
            UserName = $"{id}@test.com", Email = $"{id}@test.com"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    public static async Task<Laundry> SeedLaundryAsync(NadeefDbContext ctx, int id = 1)
    {
        var laundry = new Laundry
        {
            Id = id, Name = "Ndeef Test", Address = "Test Address",
            Latitude = 30.1m, Longitude = 31.2m,
            Status = LaundryStatus.Active, Availability = LaundryAvailability.Open,
            AdminId = "admin-1", CreatedAt = DateTime.UtcNow
        };
        ctx.Laundries.Add(laundry);
        await ctx.SaveChangesAsync();
        return laundry;
    }

    public static async Task<CourierProfile> SeedProfileAsync(
        NadeefDbContext ctx, string courierId, int laundryId,
        int completed = 0, int cancelled = 0)
    {
        var profile = new CourierProfile
        {
            CourierId = courierId, LaundryId = laundryId,
            CompletedOrdersCount = completed, CancellationCount = cancelled,
            IsAvailable = true
        };
        ctx.CourierProfiles.Add(profile);
        await ctx.SaveChangesAsync();
        return profile;
    }

    public static async Task<Order> SeedOrderAsync(
        NadeefDbContext ctx,
        string courierId,
        int laundryId,
        OrderStatus status,
        decimal price = 100m,
        bool isPaid = false,
        string customerId = "customer-1",
        int orderId = 0)
    {
        // ensure customer exists
        if (!ctx.Users.Any(u => u.Id == customerId))
        {
            ctx.Users.Add(new ApplicationUser
            {
                Id = customerId, Name = "Test Customer",
                PhoneNumber = "01000000000", Role = UserRole.Customer, IsActive = true,
                UserName = $"{customerId}@test.com", Email = $"{customerId}@test.com"
            });
        }

        var order = new Order
        {
            PickupLocation   = "Laundry Address",
            DeliveryLocation = "Nasr City, Cairo",
            PickupTime       = DateTime.UtcNow.AddHours(-2),
            Status           = status,
            TotalPrice       = price,
            CustomerId       = customerId,
            CourierId        = courierId,
            LaundryId        = laundryId,
            CreatedAt        = DateTime.UtcNow.AddHours(-3),
        };

        if (orderId > 0) order.Id = orderId;

        var payment = new Payment
        {
            Amount        = price,
            PaymentStatus = isPaid ? PaymentStatus.Paid : PaymentStatus.Pending,
            PaymentMethod = isPaid ? PaymentMethod.CreditCard : PaymentMethod.Cash,
            Order         = order
        };
        order.Payment = payment;

        if (status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow.AddMinutes(-30);
        if (status == OrderStatus.PickedUp)
            order.PickedUpAt = DateTime.UtcNow.AddHours(-1);

        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        return order;
    }
}

// ═══════════════════════════════════════════════════════════════════
//  1. GetAssignedOrdersAsync
// ═══════════════════════════════════════════════════════════════════
public class CourierService_GetAssignedOrders
{
    [Fact]
    public async Task Returns_OnlyThisCouriersOrders()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry  = await CourierTestHelpers.SeedLaundryAsync(ctx);
        var _courier = await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);

        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.ReadyForPickup, 80m);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c2", laundry.Id, OrderStatus.ReadyForPickup, 90m); // other courier

        var result = await svc.GetAssignedOrdersAsync("c1",
            new CourierOrderParams { Filter = CourierOrderFilter.Pending, PageIndex = 1, PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Data!, o => Assert.Equal(OrderStatus.ReadyForPickup, o.Status));
    }

    [Fact]
    public async Task Returns_IsPaid_True_WhenPaymentIsPaid()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.ReadyForPickup, 100m, isPaid: true);

        var result = await svc.GetAssignedOrdersAsync("c1",
            new CourierOrderParams { Filter = CourierOrderFilter.Pending, PageIndex = 1, PageSize = 10 });

        Assert.True(result.Data!.First().IsPaid);
    }

    [Fact]
    public async Task Returns_ItemsCount_FromOrderItems()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.ReadyForPickup);

        // Add 3 items
        ctx.Set<OrderItem>().Add(new OrderItem { OrderId = order.Id, ServiceId = 1, Quantity = 3, UnitPrice = 20m, Subtotal = 60m });
        await ctx.SaveChangesAsync();

        var result = await svc.GetAssignedOrdersAsync("c1",
            new CourierOrderParams { Filter = CourierOrderFilter.Pending, PageIndex = 1, PageSize = 10 });

        Assert.Equal(3, result.Data!.First().ItemsCount);
    }

    [Fact]
    public async Task Returns_EmptyPage_WhenNoPendingOrders()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.GetAssignedOrdersAsync("nonexistent",
            new CourierOrderParams { Filter = CourierOrderFilter.Pending, PageIndex = 1, PageSize = 10 });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data!);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  2. GetActiveRunAsync
// ═══════════════════════════════════════════════════════════════════
public class CourierService_GetActiveRun
{
    [Fact]
    public async Task Returns_Failure_WhenNoActiveOrders()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.GetActiveRunAsync("no-orders-courier");

        Assert.False(result.IsSuccess);
        Assert.Contains("No active run", result.ErrorMessage);
    }

    [Fact]
    public async Task Returns_ActiveRun_WithPickedUpOrder()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp, 150m);

        var result = await svc.GetActiveRunAsync("c1");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalStops);
        Assert.Single(result.Value.Stops);
        Assert.Equal("current", result.Value.Stops[0].Status);
        Assert.Equal(150m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Returns_ActiveRun_WithPendingOrders()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.ReadyForPickup, 80m);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.ReadyForPickup, 70m);

        var result = await svc.GetActiveRunAsync("c1");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalStops);
        Assert.Equal(150m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Stop_IsPaid_ReflectsPaymentStatus()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp, 100m, isPaid: true);

        var result = await svc.GetActiveRunAsync("c1");

        Assert.True(result.Value!.Stops[0].IsPaid);
    }

    [Fact]
    public async Task RunId_ContainsCourierId()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp);

        var result = await svc.GetActiveRunAsync("c1");

        Assert.Contains("c1", result.Value!.RunId);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  3. DeliverStopAsync
// ═══════════════════════════════════════════════════════════════════
public class CourierService_DeliverStop
{
    [Fact]
    public async Task Returns_Failure_WhenOrderIdIsNotInt()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.DeliverStopAsync("c1", "run1", "not-an-int",
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid order ID", result.ErrorMessage);
    }

    [Fact]
    public async Task Returns_Failure_WhenOrderNotFound()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.DeliverStopAsync("c1", "run1", "9999",
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Returns_Failure_WhenOrderBelongsToOtherCourier()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp);

        var result = await svc.DeliverStopAsync("c2", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        Assert.False(result.IsSuccess);
        Assert.Contains("not assigned to you", result.ErrorMessage);
    }

    [Fact]
    public async Task Delivers_Order_AndUpdatesStatusToDelivered()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp);

        var result = await svc.DeliverStopAsync("c1", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        Assert.True(result.IsSuccess);

        var updated = await ctx.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Delivered, updated!.Status);
        Assert.NotNull(updated.DeliveredAt);
    }

    [Fact]
    public async Task Delivers_IncrementsCourierCompletedCount()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id, completed: 5);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp);

        await svc.DeliverStopAsync("c1", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        var profile = await ctx.CourierProfiles.FirstAsync(p => p.CourierId == "c1");
        Assert.Equal(6, profile.CompletedOrdersCount);
    }

    [Fact]
    public async Task Delivers_WithCashCollected_MarksPaymentAsPaid()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.PickedUp, isPaid: false);

        await svc.DeliverStopAsync("c1", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = true, ActualDeliveryTime = DateTime.UtcNow });

        var payment = await ctx.Set<Payment>().FirstAsync(p => p.OrderId == order.Id);
        Assert.Equal(PaymentStatus.Paid, payment.PaymentStatus);
    }

    [Fact]
    public async Task Delivers_Idempotent_WhenAlreadyDelivered()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Delivered);

        var result = await svc.DeliverStopAsync("c1", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        // Already delivered → should succeed (idempotent)
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Returns_Failure_WhenOrderInWrongStatus()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Cancelled);

        var result = await svc.DeliverStopAsync("c1", "run1", order.Id.ToString(),
            new DeliverStopRequestDto { CashCollected = false, ActualDeliveryTime = DateTime.UtcNow });

        Assert.False(result.IsSuccess);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  4. GetEarningsStatsAsync
// ═══════════════════════════════════════════════════════════════════
public class CourierService_GetEarnings
{
    [Fact]
    public async Task Returns_ZeroEarnings_WhenNoDeliveredOrders()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.GetEarningsStatsAsync("no-orders");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.TotalEarned);
        Assert.Equal(0, result.Value.OrdersDone);
        Assert.Equal(7, result.Value.DailyEarnings.Count); // always 7 days
    }

    [Fact]
    public async Task Returns_CorrectWeeklyEarnings()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id, completed: 2);

        // Two delivered orders this week
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Delivered, 200m);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Delivered, 100m);

        var result = await svc.GetEarningsStatsAsync("c1");

        Assert.True(result.IsSuccess);
        // Flat fee per order: 20 EGP * 2 = 40 EGP
        Assert.Equal(40m, result.Value!.TotalEarned);
        Assert.Equal(2, result.Value.OrdersDone);
    }

    [Fact]
    public async Task Returns_CompletionRate_FromProfile()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        // 8 completed, 2 cancelled → 80%
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id, completed: 8, cancelled: 2);

        var result = await svc.GetEarningsStatsAsync("c1");

        Assert.True(result.IsSuccess);
        Assert.Equal(80.0, result.Value!.CompletionRate);
    }

    [Fact]
    public async Task Returns_7DailyEntries_AlwaysMondayToSunday()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.GetEarningsStatsAsync("c1");

        var days = result.Value!.DailyEarnings.Select(d => d.Day).ToList();
        Assert.Equal(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, days);
    }

    [Fact]
    public async Task Returns_RecentTransactions_WithCorrectFormat()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Delivered, 150m);

        var result = await svc.GetEarningsStatsAsync("c1");

        Assert.NotEmpty(result.Value!.RecentTransactions);
        // Flat fee per order: 20 EGP → "+EGP 20"
        Assert.Equal("+EGP 20", result.Value.RecentTransactions.First().Amount);
    }

    [Fact]
    public async Task NextPayoutDate_IsNextMonday()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var result = await svc.GetEarningsStatsAsync("c1");

        Assert.False(string.IsNullOrEmpty(result.Value!.NextPayoutDate));
        // Should parse as a valid date
        Assert.True(DateTime.TryParse(result.Value.NextPayoutDate, out _));
    }
}

// ═══════════════════════════════════════════════════════════════════
//  5. UpdateStatusAsync
// ═══════════════════════════════════════════════════════════════════
public class CourierService_UpdateStatus
{
    [Fact]
    public async Task UpdateStatus_ToOffline_PersistsInDb()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);

        var result = await svc.UpdateStatusAsync("c1", new UpdateCourierStatusRequestDto { IsOnline = false });

        Assert.True(result.IsSuccess);
        var profile = await ctx.CourierProfiles.FirstAsync(p => p.CourierId == "c1");
        Assert.False(profile.IsAvailable);
    }

    [Fact]
    public async Task UpdateStatus_ToOnline_PersistsInDb()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        var profile = await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        profile.IsAvailable = false;
        await ctx.SaveChangesAsync();

        var result = await svc.UpdateStatusAsync("c1", new UpdateCourierStatusRequestDto { IsOnline = true });

        Assert.True(result.IsSuccess);
        var updated = await ctx.CourierProfiles.FirstAsync(p => p.CourierId == "c1");
        Assert.True(updated.IsAvailable);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsSuccess_EvenWithNoProfile()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var (svc, _)  = CourierTestHelpers.BuildService(ctx);

        // No profile seeded
        var result = await svc.UpdateStatusAsync("ghost-courier", new UpdateCourierStatusRequestDto { IsOnline = true });

        Assert.True(result.IsSuccess);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  6. CancelOrderAsync Refunds
// ═══════════════════════════════════════════════════════════════════
public class CourierService_CancelOrder
{
    [Fact]
    public async Task CancelOrder_PaidOnline_RefundsToCustomerWallet()
    {
        using var ctx = CourierTestHelpers.BuildDb();
        var uow = new LaundryPerstince.Data.Repositories.UnitOfWork(ctx);
        var store = new Mock<IUserStore<ApplicationUser>>();
        var umMock = new Mock<UserManager<ApplicationUser>>(
                            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var notifMock = new Mock<INotificationService>();
        var kashierMock = new Mock<IKashierService>();
        kashierMock.Setup(k => k.RefundWalletPaymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()))
                   .ReturnsAsync(Result<bool>.Success(true));

        var svc = new CourierService(uow, umMock.Object, notifMock.Object, kashierMock.Object);

        var laundry = await CourierTestHelpers.SeedLaundryAsync(ctx);
        await CourierTestHelpers.SeedCourierUserAsync(ctx, "c1");
        await CourierTestHelpers.SeedProfileAsync(ctx, "c1", laundry.Id);
        
        // Seed a paid order (not cash)
        var order = await CourierTestHelpers.SeedOrderAsync(ctx, "c1", laundry.Id, OrderStatus.Accepted, 150m, isPaid: true);
        
        var result = await svc.CancelOrderAsync("c1", order.Id, new CourierCancelOrderRequest { CancellationReason = "Test Cancel" });

        Assert.True(result.IsSuccess);
        kashierMock.Verify(k => k.RefundWalletPaymentAsync(order.CustomerId, order.Id, 150m), Times.Once);
    }
}
