using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public RateLimitingTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task ExceedRequestsWithinWindow_Returns429()
    {
        _client.DefaultRequestHeaders.Remove("X-Client-Id");
        _client.DefaultRequestHeaders.Add("X-Client-Id", "rate-test-1");

        // assume limit = 2 requests per 5 seconds
        var r1 = await _client.GetAsync("/api/health");
        var r2 = await _client.GetAsync("/api/health");
        var r3 = await _client.GetAsync("/api/health");

        Assert.True(r1.StatusCode == HttpStatusCode.OK || r1.StatusCode == HttpStatusCode.NoContent);
        Assert.True(r2.StatusCode == HttpStatusCode.OK || r2.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);
    }
}