using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.Wallet;
using LaundryPerstince.Data.Context;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Services.PaymentModule;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.Order;
using LaundryShared.DTO.PaymentKashier;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums.WalletEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LaundryApi.Tests;

public class PaymentServiceTests
{
    private static NadeefDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<NadeefDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NadeefDbContext(options);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CashPaymentMethod_UpdatesPaymentStatusToPaid_PreservesOrderStatusAsPendingConfirmation()
    {
        // 1. Arrange
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var mockKashierService = new Mock<IKashierService>();
        mockKashierService.Setup(s => s.CreateCashPaymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var service = new PaymentService(
            unitOfWork,
            mockKashierService.Object);

        var customerId = "customer-123";
        var orderId = 300;
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

        var request = new ProcessPaymentRequest
        {
            OrderId = orderId,
            PaymentMethod = PaymentMethod.Cash,
            Amount = 600m
        };

        // 2. Act
        var result = await service.ProcessPaymentAsync(customerId, request);

        // 3. Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.PendingConfirmation, updatedOrder!.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.Payment?.PaymentStatus);
        Assert.Equal(PaymentMethod.Cash, updatedOrder.Payment?.PaymentMethod);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WalletPaymentMethod_UpdatesPaymentStatusToPaid_PreservesOrderStatusAsPendingConfirmation()
    {
        // 1. Arrange
        using var context = BuildDbContext();
        var unitOfWork = new UnitOfWork(context);

        var mockKashierService = new Mock<IKashierService>();
        mockKashierService.Setup(s => s.CreateWalletPaymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var service = new PaymentService(
            unitOfWork,
            mockKashierService.Object);

        var customerId = "customer-123";
        var orderId = 301;
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

        // Seed Customer Wallet with balance
        var wallet = new CustomerWallet
        {
            CustomerId = customerId,
            Balance = 1000m,
            TotalCharged = 1000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.CustomerWallets.AddAsync(wallet);

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

        var request = new ProcessPaymentRequest
        {
            OrderId = orderId,
            PaymentMethod = PaymentMethod.Wallet,
            Amount = 600m
        };

        // 2. Act
        var result = await service.ProcessPaymentAsync(customerId, request);

        // 3. Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        await unitOfWork.CompleteAsync();

        var updatedOrder = await context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.PendingConfirmation, updatedOrder!.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.Payment?.PaymentStatus);
        Assert.Equal(PaymentMethod.Wallet, updatedOrder.Payment?.PaymentMethod);
    }
}
