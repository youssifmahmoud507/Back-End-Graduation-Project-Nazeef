using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using LaundryApi.Controllers;
using LaundryServiesAbstraction.IServices.Verification;
using Xunit;

namespace LaundryApi.Tests
{
    public class DiditWebhookControllerTests
    {
        private readonly Mock<IDiditVerificationService> _diditServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly IMemoryCache _memoryCache;

        public DiditWebhookControllerTests()
        {
            _diditServiceMock = new Mock<IDiditVerificationService>();
            _configMock = new Mock<IConfiguration>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task CallbackRedirect_UsesFrontendRedirectFromQuery()
        {
            // Arrange
            _configMock.Setup(c => c["Didit:FrontendBaseUrl"]).Returns("https://app.test.com");
            var controller = new DiditWebhookController(
                _diditServiceMock.Object,
                NullLogger<DiditWebhookController>.Instance,
                _configMock.Object,
                _memoryCache);

            var explicitRedirect = "https://app.test.com/custom-success";
            var expectedSessionId = "test-session-123";

            _diditServiceMock.Setup(s => s.SyncVerificationStatusAsync(expectedSessionId))
                .ReturnsAsync(true);

            // Act
            var result = await controller.CallbackRedirect(
                sessionId: expectedSessionId,
                session_id: null,
                verificationSessionId: null,
                frontendRedirect: explicitRedirect,
                status: "Approved");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains(explicitRedirect, redirectResult.Url);
            Assert.Contains("sessionId=test-session-123", redirectResult.Url);
            Assert.Contains("status=Approved", redirectResult.Url);
        }

        [Fact]
        public async Task CallbackRedirect_IgnoresForeignHostRedirect_ToPreventOpenRedirect()
        {
            var controller = new DiditWebhookController(
                _diditServiceMock.Object,
                new Mock<ILogger<DiditWebhookController>>().Object,
                _configMock.Object,
                _memoryCache);

            var result = await controller.CallbackRedirect(
                sessionId: null,
                session_id: null,
                verificationSessionId: null,
                frontendRedirect: "https://evil.example.com/phish",
                status: null);

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.DoesNotContain("evil.example.com", redirectResult.Url);
            Assert.StartsWith("https://app.test.com", redirectResult.Url);
        }

        [Fact]
        public async Task CallbackRedirect_UsesDiditFrontendBaseUrlFromConfig_WhenFrontendRedirectIsNull()
        {
            // Arrange
            _configMock.Setup(c => c["Didit:FrontendBaseUrl"]).Returns("https://env.test.com");
            var controller = new DiditWebhookController(
                _diditServiceMock.Object,
                NullLogger<DiditWebhookController>.Instance,
                _configMock.Object,
                _memoryCache);

            var expectedSessionId = "session-001";

            // Act
            var result = await controller.CallbackRedirect(
                sessionId: expectedSessionId,
                session_id: null,
                verificationSessionId: null,
                frontendRedirect: null,
                status: "Pending");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.StartsWith("https://env.test.com/laundry-admin/verification/success", redirectResult.Url);
            Assert.Contains("sessionId=session-001", redirectResult.Url);
            Assert.Contains("status=Pending", redirectResult.Url);
        }

        [Fact]
        public async Task CallbackRedirect_SyncsStatus_WhenSessionIdIsProvided()
        {
            // Arrange
            var controller = new DiditWebhookController(
                _diditServiceMock.Object,
                NullLogger<DiditWebhookController>.Instance,
                _configMock.Object,
                _memoryCache);

            var sessionId = "session-sync";

            // Act
            await controller.CallbackRedirect(
                sessionId: sessionId,
                session_id: null,
                verificationSessionId: null,
                frontendRedirect: null,
                status: null);

            // Assert
            _diditServiceMock.Verify(s => s.SyncVerificationStatusAsync(sessionId), Times.Once);
        }

        [Theory]
        [InlineData("sess-1", null, null)]
        [InlineData(null, "sess-1", null)]
        [InlineData(null, null, "sess-1")]
        public async Task CallbackRedirect_RecognizesAllSessionIdAliases(string? s1, string? s2, string? s3)
        {
            // Arrange
            var controller = new DiditWebhookController(
                _diditServiceMock.Object,
                NullLogger<DiditWebhookController>.Instance,
                _configMock.Object,
                _memoryCache);

            // Act
            var result = await controller.CallbackRedirect(
                sessionId: s1,
                session_id: s2,
                verificationSessionId: s3,
                frontendRedirect: null,
                status: null);

            // Assert
            _diditServiceMock.Verify(s => s.SyncVerificationStatusAsync("sess-1"), Times.Once);

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains("sessionId=sess-1", redirectResult.Url);
        }
    }
}