using LaundryApi.Controllers;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.PaymentKashier;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LaundryApi.Tests;

public class PaymentControllerTests
{
    [Fact]
    public async Task KashierCallback_UsesAlternateMerchantOrderQueryKey_ForWalletRedirectAndProcessing()
    {
        var fakeKashierService = new FakeKashierService();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kashier:FrontendBaseUrl"] = "https://www.nazeef.tech"
            })
            .Build();

        var controller = new PaymentController(
            fakeKashierService,
            configuration,
            NullLogger<PaymentController>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("www.nazeef.tech");
        httpContext.Request.QueryString = new QueryString(
            "?status=success&merchant_order_id=wallet-charge-customer-123-987654321&paymentAmount=250.00");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.KashierCallback("success", null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(
            "https://www.nazeef.tech/wallet?status=success&merchantOrderId=wallet-charge-customer-123-987654321",
            redirect.Url);
        Assert.Equal("wallet-charge-customer-123-987654321", fakeKashierService.LastMerchantOrderId);
        Assert.Equal(250.00m, fakeKashierService.LastAmount);
        Assert.Equal("success", fakeKashierService.LastStatus);
    }

    private sealed class FakeKashierService : IKashierService
    {
        public string? LastMerchantOrderId { get; private set; }
        public decimal LastAmount { get; private set; }
        public string? LastStatus { get; private set; }

        public Task<Result<KashierPaymentDto>> CreateWalletChargeAsync(string customerId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<bool>> CreateWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> RefundWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateMobileWalletPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<bool>> CreateCashPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateCardPaymentAsync(string customerId, int orderId, decimal amount) => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<KashierPaymentDto>> CreateCommissionPaymentAsync(string laundryAdminId, int laundryId, decimal amount, string currency = "EGP") => Task.FromResult(Result<KashierPaymentDto>.Failure("Not implemented"));
        public Task<Result<bool>> HandleWebhookAsync(string signature, KashierWebhookDto webhook) => Task.FromResult(Result<bool>.Failure("Not implemented"));
        public Task<Result<bool>> ProcessPayoutAsync(int laundryWalletId) => Task.FromResult(Result<bool>.Failure("Not implemented"));

        public Task<Result<bool>> ProcessCallbackPaymentAsync(string merchantOrderId, decimal amount, string status)
        {
            LastMerchantOrderId = merchantOrderId;
            LastAmount = amount;
            LastStatus = status;
            return Task.FromResult(Result<bool>.Success(true));
        }
    }
}
