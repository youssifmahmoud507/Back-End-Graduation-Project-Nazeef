using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class IdempotencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public IdempotencyTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PostOrder_WithSameIdempotencyKey_ReturnsSameResponse()
    {
        var payload = new StringContent("{\"customerId\":123}", Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", "test-key-1");

        var r1 = await _client.PostAsync("/api/orders", payload);
        var body1 = await r1.Content.ReadAsStringAsync();

        // Recreate payload (HttpContent can be consumed once)
        payload = new StringContent("{\"customerId\":123}", Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", "test-key-1");

        var r2 = await _client.PostAsync("/api/orders", payload);
        var body2 = await r2.Content.ReadAsStringAsync();

        Assert.Equal(r1.StatusCode, r2.StatusCode);
        Assert.Equal(body1, body2);
    }
}