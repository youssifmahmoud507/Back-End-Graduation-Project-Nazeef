using LaundryDomain.IRepositories;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.Wallet;
using LaundryDomain.Specifications.LaundrySpecsandwalletSpecs;
using LaundryDomain.Specifications.OrderSpecs;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryShared.Common;
using LaundryShared.DTO.Order;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums.WalletEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderPayment = LaundryDomain.Models.OrderModules.Payment;


namespace LaundryServies.Services.PaymentModule
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKashierService _kashierService;

        public PaymentService(IUnitOfWork unitOfWork, IKashierService kashierService)
        {
            _unitOfWork = unitOfWork;
            _kashierService = kashierService;
        }

        // =========================================================
        // Helper: جلب أو إنشاء محفظة العميل
        // =========================================================
        private async Task<CustomerWallet> GetOrCreateWalletAsync(string customerId)
        {
            var wallet = await _unitOfWork.Repository<CustomerWallet, int>()
                .GetEntityWithSpecAsync(new CustomerWalletByCustomerIdSpec(customerId));

            if (wallet is null)
            {
                wallet = new CustomerWallet
                {
                    CustomerId = customerId,
                    Balance = 0,
                    TotalCharged = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<CustomerWallet, int>().AddAsync(wallet);
                await _unitOfWork.CompleteAsync();
            }

            return wallet;
        }

        // =========================================================
        // POST /api/payments/process
        // =========================================================
        public async Task<Result<PaymentDto>> ProcessPaymentAsync(string customerId, ProcessPaymentRequest request)
        {
            // 1. جلب الأوردر
            var spec = new OrderWithDetailsSpec(request.OrderId, customerId);
            var order = await _unitOfWork.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<PaymentDto>.Failure("Order not found.");

            if (order.Payment.PaymentStatus == PaymentStatus.Paid)
                return Result<PaymentDto>.Failure("Order already paid.");

            if (order.Payment.Amount != request.Amount)
                return Result<PaymentDto>.Failure("Payment amount does not match order total.");

            // 2. حساب خصم المحفظة التلقائي
            var customerWallet = await GetOrCreateWalletAsync(customerId);
            var walletDeduction = customerWallet.IsActive
                ? Math.Min(customerWallet.Balance, order.Payment.Amount)
                : 0m;
            var amountDue = order.Payment.Amount - walletDeduction;

            // 3. تطبيق خصم المحفظة إذا كان هناك رصيد
            if (walletDeduction > 0)
            {
                var walletResult = await _kashierService.CreateWalletPaymentAsync(customerId, request.OrderId, walletDeduction);
                if (!walletResult.IsSuccess)
                    return Result<PaymentDto>.Failure($"Wallet deduction failed: {walletResult.ErrorMessage}");
            }

            string? paymentUrl = null;

            // 4. معالجة المبلغ المتبقي
            if (amountDue == 0)
            {
                // المحفظة غطّت كل المبلغ — الدفع مكتمل لكن الأوردر ينتظر قبول المغسلة
                order.Payment.PaymentStatus = PaymentStatus.Paid;
                order.Payment.PaymentMethod = PaymentMethod.Wallet;
                order.Payment.PaymentDate = DateTime.UtcNow;
                order.Payment.UpdatedAt = DateTime.UtcNow;
                // OrderStatus يظل PendingConfirmation — المغسلة لازم تقبل الطلب
                order.UpdatedAt = DateTime.UtcNow;
            }
            else if (request.PaymentMethod == PaymentMethod.CreditCard)
            {
                var kashierResult = await _kashierService.CreateCardPaymentAsync(customerId, request.OrderId, amountDue);
                if (!kashierResult.IsSuccess)
                    return Result<PaymentDto>.Failure(kashierResult.ErrorMessage);

                order.Payment.PaymentMethod = PaymentMethod.CreditCard;
                order.Payment.UpdatedAt = DateTime.UtcNow;
                paymentUrl = kashierResult.Value.CheckoutUrl;
            }
            else // Cash
            {
                var kashierResult = await _kashierService.CreateCashPaymentAsync(customerId, request.OrderId, amountDue);
                if (!kashierResult.IsSuccess)
                    return Result<PaymentDto>.Failure(kashierResult.ErrorMessage);

                order.Payment.PaymentStatus = PaymentStatus.Paid;
                order.Payment.PaymentMethod = PaymentMethod.Cash;
                order.Payment.PaymentDate = DateTime.UtcNow;
                order.Payment.UpdatedAt = DateTime.UtcNow;
                // OrderStatus يظل PendingConfirmation — المغسلة لازم تقبل الطلب
                order.UpdatedAt = DateTime.UtcNow;
            }

            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            return Result<PaymentDto>.Success(new PaymentDto
            {
                Id = order.Payment.Id,
                Amount = order.Payment.Amount,
                WalletDeduction = walletDeduction,
                AmountDue = amountDue,
                PaymentStatus = order.Payment.PaymentStatus,
                PaymentMethod = order.Payment.PaymentMethod,
                PaymentDate = order.Payment.PaymentDate,
                PaymentUrl = paymentUrl
            });
        }

        // POST /api/payments/confirm
        public async Task<Result<PaymentDto>> ConfirmPaymentAsync(string customerId, int paymentId)
        {
            var payment = await _unitOfWork.Repository<OrderPayment, int>().GetByIdAsync(paymentId);

            if (payment == null)
                return Result<PaymentDto>.Failure("Payment not found.");

            if (payment.PaymentStatus == PaymentStatus.Paid)
                return Result<PaymentDto>.Failure("Payment already confirmed.");

            payment.PaymentStatus = PaymentStatus.Paid;
            payment.PaymentDate = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<OrderPayment, int>().Update(payment);
            await _unitOfWork.CompleteAsync();

            return Result<PaymentDto>.Success(new PaymentDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                PaymentStatus = payment.PaymentStatus,
                PaymentMethod = payment.PaymentMethod,
                PaymentDate = payment.PaymentDate
            });
        }

        // GET /api/payments/methods
        public async Task<Result<List<string>>> GetPaymentMethodsAsync()
        {
            // إرجاع طرق الدفع المتاحة (بدون Wallet كطريقة دفع مستقلة)
            var methods = new List<string> { "CreditCard", "Cash" };
            return Result<List<string>>.Success(methods);
        }

        // GET /api/payments/history
        public async Task<Result<List<PaymentDto>>> GetPaymentHistoryAsync(string customerId)
        {
            var spec = new OrdersByCustomerSpec(customerId, new OrderParams());

            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecAsync(spec);

            var payments = orders
                .Where(o => o.Payment != null)
                .Select(o => new PaymentDto
                {
                    Id = o.Payment.Id,
                    Amount = o.Payment.Amount,
                    PaymentStatus = o.Payment.PaymentStatus,
                    PaymentMethod = o.Payment.PaymentMethod,
                    PaymentDate = o.Payment.PaymentDate
                }).ToList();

            return Result<List<PaymentDto>>.Success(payments);
        }

        // GET /api/orders/{id}/payment
        public async Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId)
        {
            var spec = new OrderWithDetailsSpec(orderId, customerId);

            var order = await _unitOfWork.Repository<Order, int>().GetEntityWithSpecAsync(spec);

            if (order == null)
                return Result<PaymentDto>.Failure("Order not found.");

            if (order.Payment == null)
                return Result<PaymentDto>.Failure("No payment found for this order.");

            return Result<PaymentDto>.Success(new PaymentDto
            {
                Id = order.Payment.Id,
                Amount = order.Payment.Amount,
                PaymentStatus = order.Payment.PaymentStatus,
                PaymentMethod = order.Payment.PaymentMethod,
                PaymentDate = order.Payment.PaymentDate
            });
        }
    }
}
