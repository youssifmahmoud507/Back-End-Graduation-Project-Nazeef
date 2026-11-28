using LaundryApi.Controllers;
using LaundryServies.Services.Payment;
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
                ["Kashier:FrontendBaseUrl"] = "https://www.nazeef.tech",
                ["Kashier:PaymentApiKey"] = "test-secret"
            })
            .Build();

        var controller = new PaymentController(
            fakeKashierService,
            configuration,
            NullLogger<PaymentController>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("www.nazeef.tech");
        const string signedPart = "status=success&merchant_order_id=wallet-charge-customer-123-987654321&paymentAmount=250.00";
        var signature = KashierSignatureVerifier.ComputeHmacSha256Hex(signedPart, "test-secret");
        httpContext.Request.QueryString = new QueryString($"?{signedPart}&signature={signature}&mode=test");

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

    [Fact]
    public async Task KashierCallback_WithoutValidSignature_DoesNotProcessPayment_AsSuccess()
    {
        var fakeKashierService = new FakeKashierService();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kashier:FrontendBaseUrl"] = "https://www.nazeef.tech",
                ["Kashier:PaymentApiKey"] = "test-secret"
            })
            .Build();

        var controller = new PaymentController(fakeKashierService, configuration, NullLogger<PaymentController>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("www.nazeef.tech");
        httpContext.Request.QueryString = new QueryString(
            "?status=success&merchant_order_id=wallet-charge-customer-123-987654321&paymentAmount=250.00&signature=forged");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.KashierCallback("success", null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("status=failed", redirect.Url);
        Assert.NotEqual("success", fakeKashierService.LastStatus);
    }

    [Fact]
    public async Task KashierWebhook_WithInvalidSignature_ReturnsUnauthorized()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Kashier:PaymentApiKey"] = "test-secret" })
            .Build();
        var controller = new PaymentController(new FakeKashierService(), configuration, NullLogger<PaymentController>.Instance);
        var httpContext = new DefaultHttpContext();
        var body = "{\"event\":\"pay\",\"data\":{\"status\":\"SUCCESS\",\"signatureKeys\":[\"status\"]}}";
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.KashierWebhook("bad-signature");

        Assert.IsType<UnauthorizedObjectResult>(result);
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
