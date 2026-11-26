using LaundryShared.Common;
using LaundryShared.DTO.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Payment
{
    public interface IPaymentService
    {
        // POST /api/payments/process
        Task<Result<PaymentDto>> ProcessPaymentAsync(string customerId, ProcessPaymentRequest request);

        // POST /api/payments/confirm
        Task<Result<PaymentDto>> ConfirmPaymentAsync(string customerId, int paymentId);

        // GET /api/payments/methods
        Task<Result<List<string>>> GetPaymentMethodsAsync();

        // GET /api/payments/history
        Task<Result<List<PaymentDto>>> GetPaymentHistoryAsync(string customerId);

        // GET /api/orders/{id}/payment
        Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId);
    }
}
