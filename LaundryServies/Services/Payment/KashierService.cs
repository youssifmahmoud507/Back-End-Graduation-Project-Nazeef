using LaundryDomain.IRepositories;
using LaundryDomain.Models.Wallet;
using LaundryDomain.Specifications.LaundrySpecsandwalletSpecs;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.PaymentKashier;
using LaundryShared.Enums.WalletEnums;
using LaundryShared.Enums.CustomerEnums;
using LaundryDomain.Specifications.OrderSpecs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using LaundryDomain.Models.UserModules;

namespace LaundryServies.Services.Payment
{
    /// <summary>
    /// بيتعامل مع كل عمليات الدفع عن طريق Kashier
    /// </summary>
    public class KashierService : IKashierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KashierService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private string MerchantId => _configuration["Kashier:MerchantId"];
        private string ApiKey => _configuration["Kashier:ApiKey"];
        private string PaymentApiKey => _configuration["Kashier:PaymentApiKey"];
        private decimal CommissionRate => decimal.Parse(_configuration["Kashier:CommissionRate"] ?? "10") / 100;
        private string CheckoutUrlBase => _configuration["Kashier:CheckoutUrl"] ?? "https://checkout.kashier.io";
        private string BaseUrl => _configuration["Kashier:BaseUrl"] ?? "https://test-api.kashier.io";
        private string Mode => _configuration["Kashier:Mode"] ?? "test";
        private string FrontendBaseUrl => _configuration["Kashier:FrontendBaseUrl"] ?? "http://localhost:3000";

        public KashierService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<KashierService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
        }

        private string ComputeHmacSha256(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private async Task<LaundryWallet> GetOrCreateLaundryWalletAsync(int laundryId)
        {
            var laundryWallet = await _unitOfWork.Repository<LaundryWallet, int>()
                .GetEntityWithSpecAsync(new LaundryWalletByLaundryIdSpec(laundryId));

            if (laundryWallet is null)
            {
                laundryWallet = new LaundryWallet
                {
                    LaundryId = laundryId,
                    TotalEarnings = 0,
                    AvailableBalance = 0,
                    PendingCommission = 0,
                    DebtCeiling = 200,
                    Status = LaundryWalletStatus.Active
                };
                await _unitOfWork.Repository<LaundryWallet, int>().AddAsync(laundryWallet);
                await _unitOfWork.CompleteAsync();
            }

            return laundryWallet;
        }

        private string GenerateCheckoutUrl(string orderId, decimal amount, string currency = "EGP")
        {
            throw new NotSupportedException("V2 Checkout URL is deprecated. Use V3 Sessions.");
        }

        private async Task<string> CreateV3SessionAsync(string customerId, string orderId, decimal amount, string currency = "EGP")
        {
            var url = $"{BaseUrl}/v3/payment/sessions";
            // Kashier often rejects localhost or HTTP. Using the actual production URL or HTTPS.
            var baseUrlUi = FrontendBaseUrl.Contains("localhost") ? "https://ndeefapp-api.icydune-2fcf3dd1.germanywestcentral.azurecontainerapps.io" : FrontendBaseUrl;
            var baseUrlApi = "https://ndeefapp-api.icydune-2fcf3dd1.germanywestcentral.azurecontainerapps.io"; // Always use remote server for webhooks when testing from localhost
            
            var redirectUrl = $"{baseUrlApi}/payment/callback?merchantOrderId={orderId}&amount={amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";

            var user = await _userManager.FindByIdAsync(customerId);
            var customerPayload = new 
            {
                reference = customerId,
                first_name = user != null && !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : "Laundry",
                last_name = user != null && !string.IsNullOrWhiteSpace(user.LastName) ? user.LastName : "Customer",
                email = user != null && !string.IsNullOrWhiteSpace(user.Email) ? user.Email : "customer@ndeefapp.com",
                phone = user != null && !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "01000000000"
            };

            var payload = new
            {
                expireAt = DateTime.UtcNow.AddMinutes(30),
                maxFailureAttempts = 3,
                paymentType = "credit",
                amount = amount.ToString("F2"),
                currency,
                order = orderId,
                merchantRedirect = redirectUrl, // Redirects here on SUCCESS
                failureRedirect = true, // Redirect to merchantRedirect on failure
                // Use the application's payment webhook endpoint so Kashier callbacks reach our HandleWebhookAsync
                serverWebhook = $"{baseUrlApi}/api/payment/webhook",
                display = "en",
                type = "one-time",
                allowedMethods = "card,wallet,bank_installments,fawry,instapay,souhoola,valu",
                customer = customerPayload,
                merchantId = MerchantId
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", ApiKey); // V3 requires Secret API key in Authorization header
            client.DefaultRequestHeaders.Add("api-key", PaymentApiKey); // Kashier V3 requires Payment API Key here

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Kashier V3 Session failed: {Status} - {Body}", response.StatusCode, responseBody);
                throw new Exception($"Kashier error: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            // The v3 session response has sessionUrl at the root
            if (doc.RootElement.TryGetProperty("sessionUrl", out var element))
            {
                return element.GetString();
            }

            throw new Exception("sessionUrl not found in Kashier response");
        }

        public async Task<Result<KashierPaymentDto>> CreateWalletChargeAsync(string customerId, decimal amount)
        {
            try
            {
                var merchantOrderId = $"wallet-charge-{customerId}-{DateTime.UtcNow.Ticks}";
                var checkoutUrl = await CreateV3SessionAsync(customerId, merchantOrderId, amount);

                var dto = new KashierPaymentDto
                {
                    OrderId = merchantOrderId,
                    CheckoutUrl = checkoutUrl
                };

                return await Task.FromResult(Result<KashierPaymentDto>.Success(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kashier wallet charge.");
                return Result<KashierPaymentDto>.Failure($"Payment initialization failed: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CreateWalletPaymentAsync(string customerId, int orderId, decimal amount)
        {
            var customerWallet = await _unitOfWork.Repository<CustomerWallet, int>()
                .GetEntityWithSpecAsync(new CustomerWalletByCustomerIdSpec(customerId));

            if (customerWallet is null)
            {
                customerWallet = new CustomerWallet
                {
                    CustomerId = customerId,
                    Balance = 0,
                    TotalCharged = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<CustomerWallet, int>().AddAsync(customerWallet);
                await _unitOfWork.CompleteAsync();
            }

            if (!customerWallet.IsActive)
                return Result<bool>.Failure("محفظة العميل غير متاحة.");

            if (customerWallet.Balance < amount)
                return Result<bool>.Failure("رصيد المحفظة غير كافٍ.");

            var orderSpec = new OrderWithDetailsSpec(orderId, customerId);
            var order = await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>()
                .GetEntityWithSpecAsync(orderSpec);

            if (order is null)
                return Result<bool>.Failure("الأوردر غير موجود.");

            var laundryWallet = await GetOrCreateLaundryWalletAsync(order.LaundryId);

            // خصم من العميل
            customerWallet.Balance -= amount;

            decimal subtotal = order.TotalPrice;
            if (order.IsBundleOrder && order.BundleOrderMetadataId.HasValue)
            {
                var metadata = await _unitOfWork.Repository<LaundryDomain.Models.BundleModules.BundleOrderMetadata, Guid>()
                    .GetByIdAsync(order.BundleOrderMetadataId.Value);
                if (metadata != null)
                {
                    subtotal = metadata.BundlePriceSnapshot;
                }
            }

            decimal totalAmount = order.Payment?.Amount ?? (subtotal * (1 + FinancialConstants.ServiceFeeRate) + FinancialConstants.DeliveryFee);
            decimal ratio = totalAmount > 0 ? (amount / totalAmount) : 1m;
            decimal proportionalSubtotal = subtotal * ratio;

            var commissionAmount = Math.Round(proportionalSubtotal * 0.13m, 2);
            var laundryAmount = Math.Round(proportionalSubtotal * 0.87m, 2);

            if (laundryWallet.PendingCommission > 0)
            {
                var deduction = Math.Min(laundryWallet.PendingCommission, laundryAmount);
                laundryAmount -= deduction;
                laundryWallet.PendingCommission -= deduction;
                
                if (laundryWallet.PendingCommission <= 0)
                    laundryWallet.Status = LaundryWalletStatus.Active;
            }

            laundryWallet.AvailableBalance += laundryAmount;
            laundryWallet.TotalEarnings += laundryAmount;

            var transactionOut = new WalletTransaction
            {
                CustomerWalletId = customerWallet.Id,
                OrderId = orderId,
                Amount = amount,
                Type = TransactionType.Debit,
                Source = TransactionSource.WalletPayment,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var transactionIn = new WalletTransaction
            {
                LaundryWalletId = laundryWallet.Id,
                OrderId = orderId,
                Amount = amount,
                CommissionRate = 13m,
                CommissionAmount = commissionAmount,
                Type = TransactionType.Credit,
                Source = TransactionSource.WalletPayment,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<CustomerWallet, int>().Update(customerWallet);
            _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transactionOut);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transactionIn);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> RefundWalletPaymentAsync(string customerId, int orderId, decimal amount)
        {
            var customerWallet = await _unitOfWork.Repository<CustomerWallet, int>()
                .GetEntityWithSpecAsync(new CustomerWalletByCustomerIdSpec(customerId));

            if (customerWallet is null)
            {
                customerWallet = new CustomerWallet
                {
                    CustomerId = customerId,
                    Balance = 0,
                    TotalCharged = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<CustomerWallet, int>().AddAsync(customerWallet);
                await _unitOfWork.CompleteAsync();
            }

            var order = await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>()
                .GetByIdAsync(orderId);

            if (order is null)
                return Result<bool>.Failure("Order not found.");

            var laundryWallet = await GetOrCreateLaundryWalletAsync(order.LaundryId);

            customerWallet.Balance += amount;

            var commissionAmount = amount * CommissionRate;
            var laundryAmount = amount - commissionAmount;

            laundryWallet.AvailableBalance -= laundryAmount;
            laundryWallet.TotalEarnings -= laundryAmount;

            var refundTxOut = new WalletTransaction
            {
                CustomerWalletId = customerWallet.Id,
                OrderId = orderId,
                Amount = amount,
                Type = TransactionType.Credit,
                Source = TransactionSource.Refund,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var refundTxIn = new WalletTransaction
            {
                LaundryWalletId = laundryWallet.Id,
                OrderId = orderId,
                Amount = amount,
                CommissionRate = CommissionRate * 100,
                CommissionAmount = commissionAmount,
                Type = TransactionType.Debit,
                Source = TransactionSource.Refund,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<CustomerWallet, int>().Update(customerWallet);
            _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(refundTxOut);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(refundTxIn);
            
            return Result<bool>.Success(true);
        }

        public async Task<Result<KashierPaymentDto>> CreateCommissionPaymentAsync(string laundryAdminId, int laundryId, decimal amount, string currency = "EGP")
        {
            try
            {
                var merchantOrderId = $"commission-{laundryId}-{DateTime.UtcNow.Ticks}";
                var checkoutUrl = await CreateV3SessionAsync(laundryAdminId, merchantOrderId, amount, currency);

                var commissionPayment = new CommissionPayment
                {
                    LaundryId = laundryId,
                    LaundryAdminId = laundryAdminId,
                    Amount = amount,
                    Currency = currency,
                    Provider = "Kashier",
                    MerchantOrderId = merchantOrderId,
                    Status = CommissionPaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<CommissionPayment, int>().AddAsync(commissionPayment);
                await _unitOfWork.CompleteAsync();

                return Result<KashierPaymentDto>.Success(new KashierPaymentDto
                {
                    OrderId = merchantOrderId,
                    CheckoutUrl = checkoutUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kashier commission payment.");
                return Result<KashierPaymentDto>.Failure($"Commission payment initialization failed: {ex.Message}");
            }
        }

        public async Task<Result<KashierPaymentDto>> CreateMobileWalletPaymentAsync(string customerId, int orderId, decimal amount)
        {
            try
            {
                var merchantOrderId = $"order-{orderId}-{DateTime.UtcNow.Ticks}";
                var checkoutUrl = await CreateV3SessionAsync(customerId, merchantOrderId, amount);

                var dto = new KashierPaymentDto
                {
                    OrderId = merchantOrderId,
                    CheckoutUrl = checkoutUrl
                };

                return await Task.FromResult(Result<KashierPaymentDto>.Success(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kashier mobile wallet payment.");
                return Result<KashierPaymentDto>.Failure($"Mobile Wallet Payment initialization failed: {ex.Message}");
            }
        }

        public async Task<Result<KashierPaymentDto>> CreateCardPaymentAsync(string customerId, int orderId, decimal amount)
        {
            try
            {
                var merchantOrderId = $"order-{orderId}-{DateTime.UtcNow.Ticks}";
                var checkoutUrl = await CreateV3SessionAsync(customerId, merchantOrderId, amount);

                var dto = new KashierPaymentDto
                {
                    OrderId = merchantOrderId,
                    CheckoutUrl = checkoutUrl
                };

                return await Task.FromResult(Result<KashierPaymentDto>.Success(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kashier order payment.");
                return Result<KashierPaymentDto>.Failure($"Card Payment initialization failed: {ex.Message}");
            }
        }

        public async Task<Result<KashierPaymentDto>> CreateCommissionPaymentAsync(string adminId, int laundryId, decimal amount)
        {
            try
            {
                var merchantOrderId = $"commission-{laundryId}-{DateTime.UtcNow.Ticks}";
                var checkoutUrl = await CreateV3SessionAsync(adminId, merchantOrderId, amount);

                var dto = new KashierPaymentDto
                {
                    OrderId = merchantOrderId,
                    CheckoutUrl = checkoutUrl
                };

                return await Task.FromResult(Result<KashierPaymentDto>.Success(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kashier commission payment.");
                return Result<KashierPaymentDto>.Failure($"Commission payment initialization failed: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CreateCashPaymentAsync(string customerId, int orderId, decimal amount)
        {
            var orderSpec = new OrderWithDetailsSpec(orderId, customerId);
            var order = await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>()
                .GetEntityWithSpecAsync(orderSpec);

            if (order is null)
                return Result<bool>.Failure("Order not found");

            var laundryWallet = await GetOrCreateLaundryWalletAsync(order.LaundryId);

            decimal subtotal = order.TotalPrice;
            if (order.IsBundleOrder && order.BundleOrderMetadataId.HasValue)
            {
                var metadata = await _unitOfWork.Repository<LaundryDomain.Models.BundleModules.BundleOrderMetadata, Guid>()
                    .GetByIdAsync(order.BundleOrderMetadataId.Value);
                if (metadata != null)
                {
                    subtotal = metadata.BundlePriceSnapshot;
                }
            }

            decimal totalAmount = order.Payment?.Amount ?? (subtotal * (1 + FinancialConstants.ServiceFeeRate) + FinancialConstants.DeliveryFee);
            decimal ratio = totalAmount > 0 ? (amount / totalAmount) : 1m;
            decimal proportionalSubtotal = subtotal * ratio;

            var commissionAmount = Math.Round(proportionalSubtotal * 0.18m, 2);

            laundryWallet.PendingCommission += commissionAmount;

            if (laundryWallet.PendingCommission >= laundryWallet.DebtCeiling)
            {
                laundryWallet.Status = LaundryWalletStatus.Suspended;
            }

            var transaction = new WalletTransaction
            {
                LaundryWalletId = laundryWallet.Id,
                OrderId = orderId,
                Amount = amount,
                CommissionRate = 18m,
                CommissionAmount = commissionAmount,
                Type = TransactionType.Credit,
                Source = TransactionSource.CashPayment,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> HandleWebhookAsync(string signature, KashierWebhookDto webhook)
        {
            try
            {
                _logger.LogInformation("[Kashier Webhook] HandleWebhookAsync called. Signature: {Signature}, Event: {Event}, Mode: {Mode}", 
                    signature, 
                    webhook?.Event, 
                    webhook?.Mode);

                if (webhook?.Data == null)
                {
                    _logger.LogWarning("[Kashier Webhook] Webhook payload data is null.");
                    return Result<bool>.Failure("Invalid webhook payload");
                }

                _logger.LogInformation("[Kashier Webhook] Payload Data - Status: {Status}, Amount: {Amount}, MerchantOrderId: {MerchantOrderId}, OrderId: {OrderId}",
                    webhook.Data.Status,
                    webhook.Data.Amount,
                    webhook.Data.MerchantOrderId,
                    webhook.Data.OrderId);

                // Case-insensitive comparison: Kashier may send "SUCCESS", "success", or "Success"
                if (!string.Equals(webhook.Data.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("[Kashier Webhook] Received non-success payment hook with status: {Status}. Acknowledging to prevent Kashier retry loop.", webhook.Data.Status);
                    // Return Success so the Controller returns 200 OK — prevents Kashier from retrying endlessly
                    return Result<bool>.Success(false);
                }

                var amount = webhook.Data.Amount;
                var commissionAmount = amount * CommissionRate;
                var laundryAmount = amount - commissionAmount;

                var merchantOrderId = webhook.Data.MerchantOrderId ?? webhook.Data.OrderId;

                if (string.IsNullOrEmpty(merchantOrderId))
                {
                    _logger.LogWarning("[Kashier Webhook] MerchantOrderId is missing.");
                    return Result<bool>.Failure("MerchantOrderId is missing");
                }

                if (merchantOrderId.StartsWith("wallet-charge"))
                {
                    // Check if this transaction was already processed (to prevent double crediting)
                    var existingTransaction = await _unitOfWork.Repository<WalletTransaction, int>()
                        .GetEntityWithSpecAsync(new LaundryDomain.Specifications.BaseSpecification<WalletTransaction, int>(t => t.PaymobTransactionId == merchantOrderId));

                    if (existingTransaction != null)
                    {
                        _logger.LogInformation("[Kashier Webhook] Wallet charge transaction with merchantOrderId {MerchantOrderId} was already processed. Skipping duplicate.", merchantOrderId);
                        return Result<bool>.Success(true);
                    }

                    var customerId = merchantOrderId.Substring("wallet-charge-".Length);
                    var lastDashIndex = customerId.LastIndexOf('-');
                    if (lastDashIndex != -1)
                    {
                        customerId = customerId.Substring(0, lastDashIndex);
                    }

                    _logger.LogInformation("[Kashier Webhook] Processing wallet charge. CustomerId: {CustomerId}, Amount: {Amount}", customerId, amount);

                    var customerWallet = await _unitOfWork.Repository<CustomerWallet, int>()
                        .GetEntityWithSpecAsync(new CustomerWalletByCustomerIdSpec(customerId));

                    if (customerWallet is null)
                    {
                        _logger.LogInformation("[Kashier Webhook] Customer wallet not found for CustomerId: {CustomerId}. Creating new wallet.", customerId);
                        customerWallet = new CustomerWallet
                        {
                            CustomerId = customerId,
                            Balance = 0,
                            TotalCharged = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<CustomerWallet, int>().AddAsync(customerWallet);
                        await _unitOfWork.CompleteAsync();
                    }

                    var oldBalance = customerWallet.Balance;
                    customerWallet.Balance += amount;
                    customerWallet.TotalCharged += amount;

                    _logger.LogInformation("[Kashier Webhook] Updating wallet. CustomerWalletId: {WalletId}, OldBalance: {OldBalance}, NewBalance: {NewBalance}", 
                        customerWallet.Id, oldBalance, customerWallet.Balance);

                    var transaction = new WalletTransaction
                    {
                        CustomerWalletId = customerWallet.Id,
                        Amount = amount,
                        CommissionRate = 0,
                        CommissionAmount = 0,
                        Type = TransactionType.Credit,
                        Source = TransactionSource.WalletCharge,
                        Status = TransactionStatus.Completed,
                        PaymobTransactionId = merchantOrderId, // Using Kashier order id
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.Repository<CustomerWallet, int>().Update(customerWallet);
                    await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
                }
            else if (merchantOrderId.StartsWith("order"))
            {
                var orderId = int.Parse(merchantOrderId.Split('-')[1]);
                var spec = new OrderWithDetailsSpec(orderId, ""); 
                
                var order = await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>()
                    .GetEntityWithSpecAsync(spec) ?? await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>().GetByIdAsync(orderId);

                if (order is null)
                {
                    _logger.LogWarning("[Kashier Webhook] Order ID {OrderId} not found in DB. Skipping status update.", orderId);
                    return Result<bool>.Success(true);
                }

                decimal subtotal = order.TotalPrice;
                if (order.IsBundleOrder && order.BundleOrderMetadataId.HasValue)
                {
                    var metadata = await _unitOfWork.Repository<LaundryDomain.Models.BundleModules.BundleOrderMetadata, Guid>()
                        .GetByIdAsync(order.BundleOrderMetadataId.Value);
                    if (metadata != null)
                    {
                        subtotal = metadata.BundlePriceSnapshot;
                    }
                }

                decimal totalAmount = order.Payment?.Amount ?? (subtotal * (1 + FinancialConstants.ServiceFeeRate) + FinancialConstants.DeliveryFee);
                decimal ratio = totalAmount > 0 ? (amount / totalAmount) : 1m;
                decimal proportionalSubtotal = subtotal * ratio;

                commissionAmount = Math.Round(proportionalSubtotal * 0.13m, 2);
                laundryAmount = Math.Round(proportionalSubtotal * 0.87m, 2);

                if (order.Payment?.PaymentStatus == PaymentStatus.Paid)
                {
                    _logger.LogInformation("[Kashier Webhook] Order {OrderId} is already processed. Ignoring duplicate webhook.", orderId);
                    return Result<bool>.Success(true);
                }
                
                if (order.Payment != null)
                {
                    order.Payment.PaymentStatus = PaymentStatus.Paid;
                    order.Payment.PaymentDate = DateTime.UtcNow;
                    order.Payment.UpdatedAt = DateTime.UtcNow;
                }
                
                // OrderStatus يظل PendingConfirmation — الدفع لا يعني قبول المغسلة للطلب
                order.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>().Update(order);

                var laundryWallet = await GetOrCreateLaundryWalletAsync(order.LaundryId);

                if (laundryWallet.PendingCommission > 0)
                {
                    var deduction = Math.Min(laundryWallet.PendingCommission, laundryAmount);
                    laundryAmount -= deduction;
                    laundryWallet.PendingCommission -= deduction;

                    if (laundryWallet.PendingCommission <= 0)
                        laundryWallet.Status = LaundryWalletStatus.Active;
                }

                laundryWallet.AvailableBalance += laundryAmount;
                laundryWallet.TotalEarnings += laundryAmount;

                var transaction = new WalletTransaction
                {
                    LaundryWalletId = laundryWallet.Id,
                    OrderId = orderId,
                    Amount = amount,
                    CommissionRate = 13m,
                    CommissionAmount = commissionAmount,
                    Type = TransactionType.Credit,
                    Source = TransactionSource.MobileWallet,
                    Status = TransactionStatus.Completed,
                    PaymobTransactionId = merchantOrderId,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
                await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
            }
            else if (merchantOrderId.StartsWith("commission"))
            {
                var laundryId = int.Parse(merchantOrderId.Split('-')[1]);
                var laundryWallet = await GetOrCreateLaundryWalletAsync(laundryId);

                var paidAmount = webhook.Data.Amount;
                laundryWallet.PendingCommission = Math.Max(0, laundryWallet.PendingCommission - paidAmount);

                if (laundryWallet.PendingCommission <= 0)
                    laundryWallet.Status = LaundryWalletStatus.Active;

                var transaction = new WalletTransaction
                {
                    LaundryWalletId = laundryWallet.Id,
                    Amount = paidAmount,
                    CommissionRate = 0,
                    CommissionAmount = paidAmount,
                    Type = TransactionType.Debit,
                    Source = TransactionSource.CashPayment,
                    Status = TransactionStatus.Completed,
                    PaymobTransactionId = merchantOrderId,
                    CreatedAt = DateTime.UtcNow
                };

                // Update commission payment row
                var commissionSpec = new CommissionPaymentByMerchantOrderIdSpec(merchantOrderId);
                var commissionPayment = await _unitOfWork.Repository<CommissionPayment, int>()
                    .GetEntityWithSpecAsync(commissionSpec);
                    
                if (commissionPayment != null && commissionPayment.Status != CommissionPaymentStatus.Paid)
                {
                    commissionPayment.Status = CommissionPaymentStatus.Paid;
                    commissionPayment.PaidAt = DateTime.UtcNow;
                    _unitOfWork.Repository<CommissionPayment, int>().Update(commissionPayment);
                }

                // Create a CommissionLog record so SuperAdmin overview and totals reflect this received commission
                try
                {
                    var commissionLog = new LaundryDomain.Models.Wallet.CommissionLog
                    {
                        LaundryId = laundryId,
                        OrderId = null,
                        OrderAmount = 0m,
                        CommissionPercentage = CommissionRate * 100,
                        CommissionAmount = paidAmount,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Repository<LaundryDomain.Models.Wallet.CommissionLog, int>().AddAsync(commissionLog);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create CommissionLog for merchantOrderId={MerchantOrderId}", merchantOrderId);
                    // do not fail the whole webhook processing because of logging failure
                }

                _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
                await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
            }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("[Kashier Webhook] HandleWebhookAsync completed successfully. Changes saved to database.");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Kashier Webhook] Fatal error processing webhook.");
                return Result<bool>.Failure($"Internal webhook error: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessPayoutAsync(int laundryWalletId)
        {
            var wallet = await _unitOfWork.Repository<LaundryWallet, int>()
                .GetByIdAsync(laundryWalletId);

            if (wallet is null)
                return Result<bool>.Failure("Wallet not found");

            if (wallet.AvailableBalance <= 0)
                return Result<bool>.Success(true);

            var payoutAmount = wallet.AvailableBalance;

            var transaction = new WalletTransaction
            {
                LaundryWalletId = wallet.Id,
                Amount = payoutAmount,
                CommissionRate = 0,
                CommissionAmount = 0,
                Type = TransactionType.Debit,
                Source = TransactionSource.Payout,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            wallet.AvailableBalance = 0;
            wallet.LastPayoutDate = DateTime.UtcNow;

            _unitOfWork.Repository<LaundryWallet, int>().Update(wallet);
            await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ProcessCallbackPaymentAsync(string merchantOrderId, decimal amount, string status)
        {
            try
            {
                _logger.LogInformation("[Kashier Callback] ProcessCallbackPaymentAsync called. MerchantOrderId: {MerchantOrderId}, Amount: {Amount}, Status: {Status}", 
                    merchantOrderId, amount, status);

                if (!string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase) && 
                    !string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("[Kashier Callback] Payment status is not success: {Status}", status);
                    return Result<bool>.Success(false);
                }

                if (string.IsNullOrEmpty(merchantOrderId))
                {
                    _logger.LogWarning("[Kashier Callback] MerchantOrderId is missing.");
                    return Result<bool>.Failure("MerchantOrderId is missing");
                }

                if (merchantOrderId.StartsWith("wallet-charge"))
                {
                    // Check if already processed
                    var existingTransaction = await _unitOfWork.Repository<WalletTransaction, int>()
                        .GetEntityWithSpecAsync(new LaundryDomain.Specifications.BaseSpecification<WalletTransaction, int>(t => t.PaymobTransactionId == merchantOrderId));

                    if (existingTransaction != null)
                    {
                        _logger.LogInformation("[Kashier Callback] Wallet charge with merchantOrderId {MerchantOrderId} already processed.", merchantOrderId);
                        return Result<bool>.Success(true);
                    }

                    var customerId = merchantOrderId.Substring("wallet-charge-".Length);
                    var lastDashIndex = customerId.LastIndexOf('-');
                    if (lastDashIndex != -1)
                    {
                        customerId = customerId.Substring(0, lastDashIndex);
                    }

                    _logger.LogInformation("[Kashier Callback] Processing wallet charge. CustomerId: {CustomerId}, Amount: {Amount}", customerId, amount);

                    var customerWallet = await _unitOfWork.Repository<CustomerWallet, int>()
                        .GetEntityWithSpecAsync(new CustomerWalletByCustomerIdSpec(customerId));

                    if (customerWallet is null)
                    {
                        _logger.LogInformation("[Kashier Callback] Customer wallet not found for CustomerId: {CustomerId}. Creating new wallet.", customerId);
                        customerWallet = new CustomerWallet
                        {
                            CustomerId = customerId,
                            Balance = 0,
                            TotalCharged = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<CustomerWallet, int>().AddAsync(customerWallet);
                        await _unitOfWork.CompleteAsync();
                    }

                    var oldBalance = customerWallet.Balance;
                    customerWallet.Balance += amount;
                    customerWallet.TotalCharged += amount;

                    _logger.LogInformation("[Kashier Callback] Updating wallet. CustomerWalletId: {WalletId}, OldBalance: {OldBalance}, NewBalance: {NewBalance}", 
                        customerWallet.Id, oldBalance, customerWallet.Balance);

                    var transaction = new WalletTransaction
                    {
                        CustomerWalletId = customerWallet.Id,
                        Amount = amount,
                        CommissionRate = 0,
                        CommissionAmount = 0,
                        Type = TransactionType.Credit,
                        Source = TransactionSource.WalletCharge,
                        Status = TransactionStatus.Completed,
                        PaymobTransactionId = merchantOrderId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.Repository<CustomerWallet, int>().Update(customerWallet);
                    await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
                }
                else if (merchantOrderId.StartsWith("order"))
                {
                    var orderId = int.Parse(merchantOrderId.Split('-')[1]);
                    var spec = new OrderWithDetailsSpec(orderId, ""); 
                    
                    var order = await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>()
                        .GetEntityWithSpecAsync(spec) ?? await _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>().GetByIdAsync(orderId);

                    if (order is null)
                    {
                        _logger.LogWarning("[Kashier Callback] Order ID {OrderId} not found in DB.", orderId);
                        return Result<bool>.Success(true);
                    }

                    decimal subtotal = order.TotalPrice;
                    if (order.IsBundleOrder && order.BundleOrderMetadataId.HasValue)
                    {
                        var metadata = await _unitOfWork.Repository<LaundryDomain.Models.BundleModules.BundleOrderMetadata, Guid>()
                            .GetByIdAsync(order.BundleOrderMetadataId.Value);
                        if (metadata != null)
                        {
                            subtotal = metadata.BundlePriceSnapshot;
                        }
                    }

                    decimal totalAmount = order.Payment?.Amount ?? (subtotal * (1 + FinancialConstants.ServiceFeeRate) + FinancialConstants.DeliveryFee);
                    decimal ratio = totalAmount > 0 ? (amount / totalAmount) : 1m;
                    decimal proportionalSubtotal = subtotal * ratio;

                    var commissionAmount = Math.Round(proportionalSubtotal * 0.13m, 2);
                    var laundryAmount = Math.Round(proportionalSubtotal * 0.87m, 2);

                    if (order.Payment?.PaymentStatus == PaymentStatus.Paid)
                    {
                        _logger.LogInformation("[Kashier Callback] Order {OrderId} is already processed.", orderId);
                        return Result<bool>.Success(true);
                    }
                    
                    if (order.Payment != null)
                    {
                        order.Payment.PaymentStatus = PaymentStatus.Paid;
                        order.Payment.PaymentDate = DateTime.UtcNow;
                        order.Payment.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    // OrderStatus يظل PendingConfirmation — الدفع لا يعني قبول المغسلة للطلب
                    order.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<LaundryDomain.Models.OrderModules.Order, int>().Update(order);

                    var laundryWallet = await GetOrCreateLaundryWalletAsync(order.LaundryId);

                    if (laundryWallet.PendingCommission > 0)
                    {
                        var deduction = Math.Min(laundryWallet.PendingCommission, laundryAmount);
                        laundryAmount -= deduction;
                        laundryWallet.PendingCommission -= deduction;

                        if (laundryWallet.PendingCommission <= 0)
                            laundryWallet.Status = LaundryWalletStatus.Active;
                    }

                    laundryWallet.AvailableBalance += laundryAmount;
                    laundryWallet.TotalEarnings += laundryAmount;

                    var transaction = new WalletTransaction
                    {
                        LaundryWalletId = laundryWallet.Id,
                        OrderId = orderId,
                        Amount = amount,
                        CommissionRate = 13m,
                        CommissionAmount = commissionAmount,
                        Type = TransactionType.Credit,
                        Source = TransactionSource.MobileWallet,
                        Status = TransactionStatus.Completed,
                        PaymobTransactionId = merchantOrderId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
                    await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
                }
                else if (merchantOrderId.StartsWith("commission"))
                {
                    var laundryId = int.Parse(merchantOrderId.Split('-')[1]);
                    var laundryWallet = await GetOrCreateLaundryWalletAsync(laundryId);

                    // Check if already processed
                    var commissionSpec = new CommissionPaymentByMerchantOrderIdSpec(merchantOrderId);
                    var commissionPayment = await _unitOfWork.Repository<CommissionPayment, int>()
                        .GetEntityWithSpecAsync(commissionSpec);
                        
                    if (commissionPayment != null && commissionPayment.Status == CommissionPaymentStatus.Paid)
                    {
                        _logger.LogInformation("[Kashier Callback] Commission payment {MerchantOrderId} already processed.", merchantOrderId);
                        return Result<bool>.Success(true);
                    }

                    var paidAmount = amount;
                    laundryWallet.PendingCommission = Math.Max(0, laundryWallet.PendingCommission - paidAmount);

                    if (laundryWallet.PendingCommission <= 0)
                        laundryWallet.Status = LaundryWalletStatus.Active;

                    var transaction = new WalletTransaction
                    {
                        LaundryWalletId = laundryWallet.Id,
                        Amount = paidAmount,
                        CommissionRate = 0,
                        CommissionAmount = paidAmount,
                        Type = TransactionType.Debit,
                        Source = TransactionSource.CashPayment,
                        Status = TransactionStatus.Completed,
                        PaymobTransactionId = merchantOrderId,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (commissionPayment != null)
                    {
                        commissionPayment.Status = CommissionPaymentStatus.Paid;
                        commissionPayment.PaidAt = DateTime.UtcNow;
                        _unitOfWork.Repository<CommissionPayment, int>().Update(commissionPayment);
                    }

                    try
                    {
                        var commissionLog = new LaundryDomain.Models.Wallet.CommissionLog
                        {
                            LaundryId = laundryId,
                            OrderId = null,
                            OrderAmount = 0m,
                            CommissionPercentage = CommissionRate * 100,
                            CommissionAmount = paidAmount,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Repository<LaundryDomain.Models.Wallet.CommissionLog, int>().AddAsync(commissionLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create CommissionLog for merchantOrderId={MerchantOrderId}", merchantOrderId);
                    }

                    _unitOfWork.Repository<LaundryWallet, int>().Update(laundryWallet);
                    await _unitOfWork.Repository<WalletTransaction, int>().AddAsync(transaction);
                }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("[Kashier Callback] ProcessCallbackPaymentAsync completed successfully. Changes saved.");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Kashier Callback] Fatal error processing callback payment.");
                return Result<bool>.Failure($"Internal callback payment error: {ex.Message}");
            }
        }
    }
}
