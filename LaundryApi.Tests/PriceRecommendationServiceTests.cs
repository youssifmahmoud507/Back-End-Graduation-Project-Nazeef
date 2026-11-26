using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LaundryServies.Services.AI;
using LaundryShared.DTO.AI;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LaundryApi.Tests
{
    public class PriceRecommendationServiceTests
    {
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        [Fact]
        public async Task GetPriceRecommendationAsync_ReturnsParsedResult_WhenApiReturnsValidJson()
        {
            var handler = new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"choices\":[{\"message\":{\"content\":\"{\\\"suggested_min_price\\\":10,\\\"suggested_max_price\\\":20,\\\"recommended_price\\\":15,\\\"reasoning\\\":\\\"???\\\"}\"}}]}",
                        Encoding.UTF8,
                        "application/json")
                });

            var httpClient = new HttpClient(handler);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Mistral:ApiKey", "test-key")
                })
                .Build();

            var service = new PriceRecommendationService(httpClient, config, NullLogger<PriceRecommendationService>.Instance, _cache);

            var result = await service.GetPriceRecommendationAsync("Shirt", ServiceCategory.Iron);

            Assert.NotNull(result);
            Assert.Equal(10m, result!.SuggestedMinPrice);
            Assert.Equal(20m, result.SuggestedMaxPrice);
            Assert.Equal(15m, result.RecommendedPrice);
            Assert.Equal("???", result.Reasoning);
        }

        [Fact]
        public async Task GetPriceRecommendationAsync_ReturnsFallback_WhenApiKeyMissing()
        {
            var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler);
            var config = new ConfigurationBuilder().Build();

            var service = new PriceRecommendationService(httpClient, config, NullLogger<PriceRecommendationService>.Instance, _cache);

            var result = await service.GetPriceRecommendationAsync("Shirt", ServiceCategory.Iron);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result!.Reasoning));
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
