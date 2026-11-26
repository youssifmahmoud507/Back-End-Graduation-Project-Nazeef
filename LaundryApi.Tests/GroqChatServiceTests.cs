using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LaundryServies.Services.Chat;
using LaundryServiesAbstraction.IServices.Order;
using LaundryShared.Common;
using LaundryShared.DTO.Chat;
using LaundryShared.DTO.Order;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaundryApi.Tests
{
    public class GroqChatServiceTests
    {
        [Fact]
        public async Task SendMessageAsync_ReturnsAssistantReply_WhenApiReturnsValidJson()
        {
            var handler = new SequencedHttpMessageHandler(new[]
            {
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"choices\":[{\"message\":{\"content\":\"????\"}}]}",
                        Encoding.UTF8,
                        "application/json")
                }
            });

            var service = CreateService(handler);

            var result = await service.SendMessageAsync(
                "user-1",
                new ChatRequestDto { Message = "?????" });

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("????", result.Value.Message);
        }

        [Fact]
        public async Task SendMessageAsync_ExecutesToolCallAndReturnsFollowUpResponse()
        {
            var handler = new SequencedHttpMessageHandler(new[]
            {
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"choices\":[{\"message\":{\"content\":null,\"tool_calls\":[{\"id\":\"call_1\",\"function\":{\"name\":\"cancel_order\",\"arguments\":\"{\\\"order_id\\\":\\\"123\\\"}\"}}]}}]}",
                        Encoding.UTF8,
                        "application/json")
                },
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"choices\":[{\"message\":{\"content\":\"?? ??????? ?????\"}}]}",
                        Encoding.UTF8,
                        "application/json")
                }
            });

            var stubOrderService = new StubOrderService();
            var services = new ServiceCollection();
            services.AddSingleton<IOrderService>(stubOrderService);
            var serviceProvider = services.BuildServiceProvider();

            var service = CreateService(handler, serviceProvider);

            var result = await service.SendMessageAsync(
                "user-1",
                new ChatRequestDto { Message = "???? ????? 123" });

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("?? ??????? ?????", result.Value.Message);
            Assert.True(stubOrderService.WasCalled);
            Assert.Equal("user-1", stubOrderService.CapturedUserId);
            Assert.Equal(123, stubOrderService.CapturedOrderId);
            Assert.Equal(2, handler.RequestCount);
        }

        private static GroqChatService CreateService(
            HttpMessageHandler handler,
            IServiceProvider? serviceProvider = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Groq:BaseUrl", "https://api.groq.test"),
                    new KeyValuePair<string, string?>("Groq:ApiKey", "test-key"),
                    new KeyValuePair<string, string?>("Groq:Model", "test-model")
                })
                .Build();

            var provider = serviceProvider ?? new ServiceCollection().BuildServiceProvider();
            var service = new GroqChatService(config, provider);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.groq.test")
            };

            var httpClientField = typeof(GroqChatService)
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            httpClientField!.SetValue(service, httpClient);

            return service;
        }

        private sealed class SequencedHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;
            public int RequestCount { get; private set; }

            public SequencedHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestCount++;
                if (_responses.Count == 0)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                }

                return Task.FromResult(_responses.Dequeue());
            }
        }

        private sealed class StubOrderService : IOrderService
        {
            public bool WasCalled { get; private set; }
            public string? CapturedUserId { get; private set; }
            public int CapturedOrderId { get; private set; }

            public Task<Result<OrderDto>> PlaceOrderAsync(string customerId, PlaceOrderRequest request) =>
                throw new NotImplementedException();

            public Task<Result<CalculatePriceResponse>> CalculatePriceAsync(CalculatePriceRequest request) =>
                throw new NotImplementedException();

            public Task<Result<Pagination<OrderDto>>> GetCustomerOrdersAsync(string customerId, OrderParams orderParams) =>
                throw new NotImplementedException();

            public Task<Result<List<OrderDto>>> GetActiveOrdersAsync(string customerId) =>
                throw new NotImplementedException();

            public Task<Result<Pagination<OrderDto>>> GetOrderHistoryAsync(string customerId, OrderParams orderParams) =>
                throw new NotImplementedException();

            public Task<Result<OrderDto>> GetOrderByIdAsync(string customerId, int orderId) =>
                throw new NotImplementedException();

            public Task<Result<OrderTrackDto>> TrackOrderAsync(string customerId, int orderId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> CancelOrderAsync(string customerId, int orderId, CancelOrderRequest request)
            {
                WasCalled = true;
                CapturedUserId = customerId;
                CapturedOrderId = orderId;
                return Task.FromResult(Result<bool>.Success(true));
            }

            public Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId) =>
                throw new NotImplementedException();
        }
    }
}
