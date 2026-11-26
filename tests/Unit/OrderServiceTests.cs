using Moq;
using Xunit;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_WhenClientHasActiveOrder_ReturnsFailure()
    {
        var repoMock = new Mock<IOrderRepository>();
        // simulate existing active order
        repoMock.Setup(r => r.HasActiveOrderAsync(It.IsAny<int>())).ReturnsAsync(true);

        var svc = new OrderService(repoMock.Object /*, other deps */);

        var result = await svc.CreateOrderAsync(new OrderRequest { CustomerId = 123 });

        Assert.False(result.Success);
        Assert.Equal("Active order exists", result.ErrorMessage); // depends on your implementation
    }
}