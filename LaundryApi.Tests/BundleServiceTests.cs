using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Services.BundleModule;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.BundleModule;
using LaundryShared.DTO.Order;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaundryApi.Tests;

file static class BundleTestHelpers
{
    public static NadeefDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NadeefDbContext(opts);
    }

    public static BundleService BuildService(NadeefDbContext ctx)
    {
        var uow = new UnitOfWork(ctx);
        var notifMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<BundleService>>();
        return new BundleService(uow, notifMock.Object, loggerMock.Object);
    }

    public static BundleOrderService BuildOrderService(NadeefDbContext ctx)
    {
        var uow = new UnitOfWork(ctx);
        var paymentMock = new Mock<IPaymentService>();
        var notifMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<BundleOrderService>>();

        notifMock.Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);

        return new BundleOrderService(uow, paymentMock.Object, notifMock.Object, loggerMock.Object);
    }

    public static async Task<Laundry> SeedLaundryAsync(NadeefDbContext ctx, int id, string adminId)
    {
        var laundry = new Laundry
        {
            Id = id,
            Name = $"Laundry {id}",
            Address = "Test Street",
            Latitude = 30.0m,
            Longitude = 31.0m,
            AdminId = adminId,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Laundries.Add(laundry);
        await ctx.SaveChangesAsync();
        return laundry;
    }

    public static async Task<Service> SeedServiceAsync(NadeefDbContext ctx, int id, int laundryId, string serviceName, ServiceCategory category, bool isAvailable = true)
    {
        var service = new Service
        {
            Id = id,
            LaundryId = laundryId,
            ServiceName = serviceName,
            Category = category,
            IsAvailable = isAvailable,
            Price = 50m
        };
        ctx.Services.Add(service);
        await ctx.SaveChangesAsync();
        return service;
    }

    public static async Task<Bundle> SeedBundleAsync(
        NadeefDbContext ctx,
        Guid id,
        string name,
        BundleStatus status,
        BundleOwnerType ownerType,
        int? laundryId,
        string createdByAdminId)
    {
        var bundle = new Bundle
        {
            Id = id,
            Name = name,
            BundlePrice = 100m,
            OriginalPrice = 150m,
            Status = status,
            OwnerType = ownerType,
            LaundryId = laundryId,
            CreatedByAdminId = createdByAdminId
        };
        ctx.Bundles.Add(bundle);
        await ctx.SaveChangesAsync();
        return bundle;
    }

    public static async Task<Bundle> SeedBundleWithItemsAsync(
        NadeefDbContext ctx,
        Guid id,
        string name,
        BundleStatus status,
        BundleOwnerType ownerType,
        int? laundryId,
        string createdByAdminId,
        List<(int serviceId, int qty, ServiceCategory? category)> items)
    {
        var bundle = new Bundle
        {
            Id = id,
            Name = name,
            BundlePrice = 100m,
            OriginalPrice = 150m,
            Status = status,
            OwnerType = ownerType,
            LaundryId = laundryId,
            CreatedByAdminId = createdByAdminId,
            BundleItems = new List<BundleItem>()
        };

        foreach (var item in items)
        {
            bundle.BundleItems.Add(new BundleItem
            {
                Id = Guid.NewGuid(),
                BundleId = id,
                ServiceId = item.serviceId,
                Quantity = item.qty,
                AllowedItemCategory = item.category
            });
        }

        ctx.Bundles.Add(bundle);
        await ctx.SaveChangesAsync();
        return bundle;
    }
}

public class BundleService_DeactivateBundle
{
    [Fact]
    public async Task SuperAdmin_CanDeactivate_AnyBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        // seed a platform bundle
        var bundleId = Guid.NewGuid();
        await BundleTestHelpers.SeedBundleAsync(ctx, bundleId, "Platform Bundle", BundleStatus.Active, BundleOwnerType.Platform, null, "superadmin-1");

        // call deactivation with adminId that does NOT own a laundry (laundry is null, hence SuperAdmin)
        var result = await svc.DeactivateBundleAsync(bundleId, "superadmin-1");

        Assert.True(result.IsSuccess);
        var bundle = await ctx.Bundles.FindAsync(bundleId);
        Assert.Equal(BundleStatus.Inactive, bundle!.Status);
    }

    [Fact]
    public async Task LaundryAdmin_CanDeactivate_TheirOwnLaundryBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        // Seed Laundry
        await BundleTestHelpers.SeedLaundryAsync(ctx, 10, "admin-10");

        // Seed Laundry Bundle
        var bundleId = Guid.NewGuid();
        await BundleTestHelpers.SeedBundleAsync(ctx, bundleId, "Laundry 10 Bundle", BundleStatus.Active, BundleOwnerType.Laundry, 10, "admin-other-or-same");

        // call deactivation by admin-10
        var result = await svc.DeactivateBundleAsync(bundleId, "admin-10");

        Assert.True(result.IsSuccess);
        var bundle = await ctx.Bundles.FindAsync(bundleId);
        Assert.Equal(BundleStatus.Inactive, bundle!.Status);
    }

    [Fact]
    public async Task LaundryAdmin_CannotDeactivate_PlatformBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        // Seed Laundry
        await BundleTestHelpers.SeedLaundryAsync(ctx, 10, "admin-10");

        // Seed Platform Bundle
        var bundleId = Guid.NewGuid();
        await BundleTestHelpers.SeedBundleAsync(ctx, bundleId, "Platform Bundle", BundleStatus.Active, BundleOwnerType.Platform, null, "superadmin-1");

        // call deactivation by admin-10
        var result = await svc.DeactivateBundleAsync(bundleId, "admin-10");

        Assert.False(result.IsSuccess);
        Assert.Contains("You can only deactivate your own laundry bundles", result.ErrorMessage);
        
        var bundle = await ctx.Bundles.FindAsync(bundleId);
        Assert.Equal(BundleStatus.Active, bundle!.Status);
    }

    [Fact]
    public async Task LaundryAdmin_CannotDeactivate_OtherLaundryBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        // Seed Laundry 10 and 20
        await BundleTestHelpers.SeedLaundryAsync(ctx, 10, "admin-10");
        await BundleTestHelpers.SeedLaundryAsync(ctx, 20, "admin-20");

        // Seed Laundry 20 Bundle
        var bundleId = Guid.NewGuid();
        await BundleTestHelpers.SeedBundleAsync(ctx, bundleId, "Laundry 20 Bundle", BundleStatus.Active, BundleOwnerType.Laundry, 20, "admin-20");

        // call deactivation by admin-10
        var result = await svc.DeactivateBundleAsync(bundleId, "admin-10");

        Assert.False(result.IsSuccess);
        Assert.Contains("You can only deactivate your own laundry bundles", result.ErrorMessage);

        var bundle = await ctx.Bundles.FindAsync(bundleId);
        Assert.Equal(BundleStatus.Active, bundle!.Status);
    }

    [Fact]
    public async Task Deactivate_Fails_WhenBundleNotFound()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        var result = await svc.DeactivateBundleAsync(Guid.NewGuid(), "admin-1");

        Assert.False(result.IsSuccess);
        Assert.Contains("Bundle not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Deactivate_Fails_WhenBundleNotActive()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var svc = BundleTestHelpers.BuildService(ctx);

        // Seed Laundry
        await BundleTestHelpers.SeedLaundryAsync(ctx, 10, "admin-10");

        // Seed an Inactive Bundle
        var bundleId = Guid.NewGuid();
        await BundleTestHelpers.SeedBundleAsync(ctx, bundleId, "Inactive Bundle", BundleStatus.Inactive, BundleOwnerType.Laundry, 10, "admin-10");

        var result = await svc.DeactivateBundleAsync(bundleId, "admin-10");

        Assert.False(result.IsSuccess);
        Assert.Contains("Only active bundles can be deactivated", result.ErrorMessage);
    }
}

public class BundleOrderService_PlaceBundleOrder
{
    [Fact]
    public async Task Customer_CanSuccessfully_OrderLaundryBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var orderService = BundleTestHelpers.BuildOrderService(ctx);

        // 1. Seed Laundry
        int laundryId = 10;
        await BundleTestHelpers.SeedLaundryAsync(ctx, laundryId, "admin-10");

        // 2. Seed Services
        await BundleTestHelpers.SeedServiceAsync(ctx, 101, laundryId, "T-Shirt Wash", ServiceCategory.Wash);
        await BundleTestHelpers.SeedServiceAsync(ctx, 102, laundryId, "Pants Iron", ServiceCategory.Iron);

        // 3. Seed Bundle with items (laundry-specific)
        var bundleId = Guid.NewGuid();
        var bundle = await BundleTestHelpers.SeedBundleWithItemsAsync(
            ctx,
            bundleId,
            "Laundry Bundle Deluxe",
            BundleStatus.Active,
            BundleOwnerType.Laundry,
            laundryId,
            "admin-10",
            new List<(int serviceId, int qty, ServiceCategory? category)>
            {
                (101, 2, ServiceCategory.Wash),
                (102, 1, ServiceCategory.Iron)
            });

        var item1Id = bundle.BundleItems.ElementAt(0).Id;
        var item2Id = bundle.BundleItems.ElementAt(1).Id;

        // 4. Place order request
        var request = new PlaceBundleOrderRequest
        {
            BundleId = bundleId,
            LaundryId = laundryId,
            PickupLocation = "123 Customer St",
            DeliveryLocation = "123 Customer St",
            ScheduledPickupTime = DateTime.UtcNow.AddDays(1),
            SelectedItems = new List<BundleItemSelectionDto>
            {
                new BundleItemSelectionDto { BundleItemId = item1Id, ServiceId = 101, Quantity = 2 },
                new BundleItemSelectionDto { BundleItemId = item2Id, ServiceId = 102, Quantity = 1 }
            }
        };

        var result = await orderService.PlaceBundleOrderAsync(request, "customer-123");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.BundleOrderMetadata);
        Assert.Equal(3, result.Value.Items.Sum(o => o.Quantity));
    }

    [Fact]
    public async Task Customer_CanSuccessfully_OrderPlatformBundle()
    {
        using var ctx = BundleTestHelpers.BuildDb();
        var orderService = BundleTestHelpers.BuildOrderService(ctx);

        // 1. Seed Laundry
        int laundryId = 10;
        await BundleTestHelpers.SeedLaundryAsync(ctx, laundryId, "admin-10");

        // 2. Seed Services matching the platform bundle's category requirements
        await BundleTestHelpers.SeedServiceAsync(ctx, 201, laundryId, "Local Laundry Wash", ServiceCategory.Wash);

        // 3. Seed Platform Bundle (laundryId is null)
        var bundleId = Guid.NewGuid();
        var bundle = await BundleTestHelpers.SeedBundleWithItemsAsync(
            ctx,
            bundleId,
            "Platform Economy Wash",
            BundleStatus.Active,
            BundleOwnerType.Platform,
            null,
            "superadmin-1",
            new List<(int serviceId, int qty, ServiceCategory? category)>
            {
                (0, 3, ServiceCategory.Wash) // generic/platform template item (ServiceId is placeholder 0, but Category Wash is required)
            });

        var itemId = bundle.BundleItems.ElementAt(0).Id;

        // 4. Place order request using the resolved local laundry service ID
        var request = new PlaceBundleOrderRequest
        {
            BundleId = bundleId,
            LaundryId = laundryId,
            PickupLocation = "456 Customer Ave",
            DeliveryLocation = "456 Customer Ave",
            ScheduledPickupTime = DateTime.UtcNow.AddDays(1),
            SelectedItems = new List<BundleItemSelectionDto>
            {
                new BundleItemSelectionDto { BundleItemId = itemId, ServiceId = 201, Quantity = 3 }
            }
        };

        var result = await orderService.PlaceBundleOrderAsync(request, "customer-123");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.BundleOrderMetadata);
        
        // Assert the order item has successfully mapped to the resolved local service ID
        var orderItem = result.Value.Items.First();
        Assert.Equal(201, orderItem.ServiceId);
        Assert.Equal(3, orderItem.Quantity);
    }
}
