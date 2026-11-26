using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.Wallet;
using LaundryPerstince.Data.Context;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Services.Payment;
using LaundryShared.Common;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums.WalletEnums;
using LaundryShared.DTO.PaymentKashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LaundryApi.Tests;

public class KashierServiceTests
{
    private static NadeefDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NadeefDbContext(options);
    }

    [Fact]
    public async Task CreateWalletPaymentAsync_AutoCreatesLaundryWallet_WhenLaundryWalletDoesNotExist()
    {
        // 1. Arrange
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kashier:CommissionRate"] = "13"
            })
            .Build();

        var service = new KashierService(
            unitOfWork,
            configuration,
            NullLogger<KashierService>.Instance,
            null!); // UserManager is not used in wallet payment

        var customerId = "customer-123";
        var orderId = 119;
        var laundryId = 5;

        // Seed customer wallet with balance
        var customerWallet = new CustomerWallet
        {
            CustomerId = customerId,
            Balance = 1000m,
            TotalCharged = 1000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.CustomerWallets.AddAsync(customerWallet);

        // Seed Laundry
        var laundry = new Laundry
        {
            Id = laundryId,
            Name = "Test Laundry",
            Address = "Test Address",
            Latitude = 30m,
            Longitude = 31m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = "admin-1",
            CreatedAt = DateTime.UtcNow
        };
        await context.Laundries.AddAsync(laundry);

        // Seed Order
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            LaundryId = laundryId,
            Status = OrderStatus.PendingConfirmation,
            TotalPrice = 600m,
            PickupLocation = "Home",
            DeliveryLocation = "Home",
            PickupTime = DateTime.UtcNow.AddDays(1),
            DeliveryTime = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            Payment = new Payment
            {
                Amount = 600m,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.CreditCard,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        // Verify that laundry wallet does NOT exist initially
        var initialWallet = await context.LaundryWallets.FirstOrDefaultAsync(w => w.LaundryId == laundryId);
        Assert.Null(initialWallet);

        // 2. Act
        var result = await service.CreateWalletPaymentAsync(customerId, orderId, 600m);

        // 3. Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);

        // Save remaining changes (updates to wallets and transactions)
        await unitOfWork.CompleteAsync();

        // Verify that laundry wallet was auto-created
        var createdWallet = await context.LaundryWallets.FirstOrDefaultAsync(w => w.LaundryId == laundryId);
        Assert.NotNull(createdWallet);
        Assert.Equal(laundryId, createdWallet!.LaundryId);
        Assert.Equal(LaundryWalletStatus.Active, createdWallet.Status);

        // Calculate expected laundry amount (87% of 600m)
        var expectedLaundryAmount = Math.Round(600m * 0.87m, 2);
        Assert.Equal(expectedLaundryAmount, createdWallet.AvailableBalance);
        Assert.Equal(expectedLaundryAmount, createdWallet.TotalEarnings);

        // Verify transaction was logged
        var transaction = await context.WalletTransactions.FirstOrDefaultAsync(t => t.LaundryWalletId == createdWallet.Id);
        Assert.NotNull(transaction);
        Assert.Equal(orderId, transaction!.OrderId);
        Assert.Equal(TransactionType.Credit, transaction.Type);
        Assert.Equal(expectedLaundryAmount, transaction.Amount - transaction.CommissionAmount);
    }

    [Fact]
    public async Task ProcessCallbackPaymentAsync_UpdatesPaymentStatusToPaid_PreservesOrderStatusAsPendingConfirmation()
    {
        // 1. Arrange
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kashier:CommissionRate"] = "13"
            })
            .Build();

        var service = new KashierService(
            unitOfWork,
            configuration,
            NullLogger<KashierService>.Instance,
            null!);

        var customerId = "customer-123";
        var orderId = 120;
        var laundryId = 5;

        // Seed Laundry
        var laundry = new Laundry
        {
            Id = laundryId,
            Name = "Test Laundry",
            Address = "Test Address",
            Latitude = 30m,
            Longitude = 31m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = "admin-1",
            CreatedAt = DateTime.UtcNow
        };
        await context.Laundries.AddAsync(laundry);

        // Seed Order
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            LaundryId = laundryId,
            Status = OrderStatus.PendingConfirmation,
            TotalPrice = 600m,
            PickupLocation = "Home",
            DeliveryLocation = "Home",
            PickupTime = DateTime.UtcNow.AddDays(1),
            DeliveryTime = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            Payment = new Payment
            {
                Amount = 600m,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.CreditCard,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        // 2. Act
        var result = await service.ProcessCallbackPaymentAsync($"order-{orderId}", 600m, "success");

        // 3. Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.PendingConfirmation, updatedOrder!.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.Payment?.PaymentStatus);
    }

    [Fact]
    public async Task HandleWebhookAsync_UpdatesPaymentStatusToPaid_PreservesOrderStatusAsPendingConfirmation()
    {
        // 1. Arrange
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kashier:CommissionRate"] = "13"
            })
            .Build();

        var service = new KashierService(
            unitOfWork,
            configuration,
            NullLogger<KashierService>.Instance,
            null!);

        var customerId = "customer-123";
        var orderId = 121;
        var laundryId = 5;

        // Seed Laundry
        var laundry = new Laundry
        {
            Id = laundryId,
            Name = "Test Laundry",
            Address = "Test Address",
            Latitude = 30m,
            Longitude = 31m,
            Status = LaundryStatus.Active,
            Availability = LaundryAvailability.Open,
            AdminId = "admin-1",
            CreatedAt = DateTime.UtcNow
        };
        await context.Laundries.AddAsync(laundry);

        // Seed Order
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            LaundryId = laundryId,
            Status = OrderStatus.PendingConfirmation,
            TotalPrice = 600m,
            PickupLocation = "Home",
            DeliveryLocation = "Home",
            PickupTime = DateTime.UtcNow.AddDays(1),
            DeliveryTime = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            Payment = new Payment
            {
                Amount = 600m,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.CreditCard,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var webhookDto = new KashierWebhookDto
        {
            Event = "shortcut.pay",
            Mode = "live",
            Data = new KashierWebhookDataDto
            {
                Status = "SUCCESS",
                Amount = 600m,
                MerchantOrderId = $"order-{orderId}",
                OrderId = $"order-{orderId}"
            }
        };

        // 2. Act
        var result = await service.HandleWebhookAsync("dummy-signature", webhookDto);

        // 3. Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.PendingConfirmation, updatedOrder!.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.Payment?.PaymentStatus);
    }
}
