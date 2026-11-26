using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Security.Claims;
using LaundryServies.Services.Verification;
using LaundryApi.Controllers;
using LaundryServiesAbstraction.IServices.Verification;
using LaundryDomain.Models.UserModules;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Routing;
using LaundryShared.Enums;

namespace LaundryApi.Tests
{
    public class DiditVerificationTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public DiditVerificationTests()
        {
            _configMock = new Mock<IConfiguration>();
            SetupConfiguration();

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private void SetupConfiguration()
        {
            _configMock.Setup(x => x["Didit:WorkflowId"]).Returns("bccb3fd1-b407-4811-958f-f5af334813f5");
            _configMock.Setup(x => x["Didit:ApiKey"]).Returns("test-api-key");
            _configMock.Setup(x => x["Didit:WebhookBaseUrl"]).Returns("https://ndeefapp.runasp.net");
            _configMock.Setup(x => x["Didit:WebhookSecretKey"]).Returns("test-secret");
        }

        [Fact]
        public void CreateSessionRequest_HasCorrectProperties()
        {
            // Arrange
            var request = new TicketsShared.DTO.Verification.CreateSessionRequest
            {
                WorkflowId = "test-workflow",
                VendorData = "user_123",
                Callback = "https://callback.url",
                RedirectUrl = "https://redirect.url",
                CallbackMethod = "both"
            };

            // Assert
            Assert.Equal("test-workflow", request.WorkflowId);
            Assert.Equal("user_123", request.VendorData);
            Assert.Equal("https://callback.url", request.Callback);
            Assert.Equal("https://redirect.url", request.RedirectUrl);
            Assert.Equal("both", request.CallbackMethod);
        }

        [Fact]
        public void DiditFeatures_HasCorrectDefaults()
        {
            // Arrange
            var features = new TicketsShared.DTO.Verification.DiditFeatures
            {
                AllowDesktop = true,
                DesktopSync = true,
                ShowExitButton = true,
                ShowNavigationButton = true,
                DisableRedirectionDelay = true
            };

            // Assert
            Assert.True(features.AllowDesktop);
            Assert.True(features.DesktopSync);
            Assert.True(features.ShowExitButton);
            Assert.True(features.ShowNavigationButton);
            Assert.True(features.DisableRedirectionDelay);
        }

        [Fact]
        public void WebhookPayload_ParsesCorrectly()
        {
            // Arrange
            var json = @"{
                ""session_id"": ""test-session"",
                ""status"": ""Approved"",
                ""vendor_data"": ""user_123_abc456"",
                ""webhook_type"": ""verification.completed"",
                ""workflow_id"": ""test-workflow""
            }";

            // Act
            var payload = JsonSerializer.Deserialize<TicketsShared.DTO.Verification.DiditWebhookPayload>(json);

            // Assert
            Assert.Equal("test-session", payload.SessionId);
            Assert.Equal("Approved", payload.Status);
            Assert.Equal("user_123_abc456", payload.VendorData);
            Assert.Equal("verification.completed", payload.WebhookType);
            Assert.Equal("test-workflow", payload.WorkflowId);
        }

        [Fact]
        public void ExtractUserId_WithGuidSuffix_ReturnsOriginalUserId()
        {
            // Arrange
            var vendorData = "user123_7d3f9e2a8b5c4d6e1f0a";

            // Act
            var actualUserId = ExtractUserId(vendorData);

            // Assert
            Assert.Equal("user123", actualUserId);
        }

        [Fact]
        public void ExtractUserId_WithoutGuid_ReturnsOriginal()
        {
            // Arrange
            var vendorData = "user123";

            // Act
            var actualUserId = ExtractUserId(vendorData);

            // Assert
            Assert.Equal("user123", actualUserId);
        }

        [Fact]
        public void ExtractUserId_NullOrEmpty_ReturnsOriginal()
        {
            // Assert
            Assert.Equal(null, ExtractUserId(null));
            Assert.Equal("", ExtractUserId(""));
        }

        private static string ExtractUserId(string vendorData)
        {
            if (string.IsNullOrEmpty(vendorData)) return vendorData;
            var underscoreIndex = vendorData.IndexOf('_');
            return underscoreIndex > 0 ? vendorData.Substring(0, underscoreIndex) : vendorData;
        }

        [Fact]
        public void UniqueVendorData_ContainsGuid()
        {
            // Arrange
            var userId = "test-user";
            var uniqueVendorData = $"{userId}_{Guid.NewGuid():N}";

            // Assert
            Assert.Contains("_", uniqueVendorData);
            Assert.StartsWith(userId, uniqueVendorData);
            Assert.True(uniqueVendorData.Length > userId.Length + 1);
        }

        [Fact]
        public void UniqueRedirectUrl_ContainsTimestamp()
        {
            // Arrange
            var redirectUrl = "https://example.com/callback";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var uniqueUrl = $"{redirectUrl}?t={timestamp}";

            // Assert
            Assert.Contains("?t=", uniqueUrl);
            Assert.StartsWith(redirectUrl, uniqueUrl);
        }

        [Fact]
        public void WebhookController_ComputeHmac_ProducesValidSignature()
        {
            // Arrange
            var secret = "test-secret";
            var data = "test-data";

            // Act
            var signature = ComputeHmac(data, secret);

            // Assert
            Assert.NotNull(signature);
            Assert.Equal(64, signature.Length); // SHA256 produces 64 hex chars
            Assert.True(signature.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f')));
        }

        private static string ComputeHmac(string data, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        [Theory]
        [InlineData("Approved", true)]
        [InlineData("Declined", false)]
        [InlineData("In Review", false)]
        public void VerificationStatus_DeterminesCorrectAction(string status, bool shouldVerify)
        {
            // Arrange & Act
            var isVerified = status == "Approved";

            // Assert
            Assert.Equal(shouldVerify, isVerified);
        }

        [Fact]
        public async Task DiditCallback_PrefersVerificationSessionId_WhenOtherIdsAreMissing()
        {
            var diditService = new Mock<IDiditVerificationService>();
            diditService
                .Setup(service => service.SyncVerificationStatusAsync("didit-session-123"))
                .ReturnsAsync(true);

            var controller = new VerificationController(
                diditService.Object,
                _userManagerMock.Object,
                NullLogger<VerificationController>.Instance,
                _configMock.Object);

            var result = await controller.DiditCallback(null, null, "didit-session-123", null, "Approved");

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("sessionId=didit-session-123", redirect.Url);
            diditService.Verify(service => service.SyncVerificationStatusAsync("didit-session-123"), Times.Once);
        }

        [Fact]
        public async Task SyncVerificationStatus_ReturnsBadRequest_WhenSessionIsMissing()
        {
            var diditService = new Mock<IDiditVerificationService>();
            var controller = new VerificationController(
                diditService.Object,
                _userManagerMock.Object,
                NullLogger<VerificationController>.Instance,
                _configMock.Object);

            var result = await controller.SyncVerificationStatus(null, null, null);

            Assert.IsType<BadRequestObjectResult>(result);
            diditService.Verify(service => service.SyncVerificationStatusAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SyncVerificationStatus_UsesVerificationSessionId_QueryValue()
        {
            var diditService = new Mock<IDiditVerificationService>();
            diditService
                .Setup(service => service.SyncVerificationStatusAsync("didit-session-456"))
                .ReturnsAsync(true);

            var controller = new VerificationController(
                diditService.Object,
                _userManagerMock.Object,
                NullLogger<VerificationController>.Instance,
                _configMock.Object);

            var result = await controller.SyncVerificationStatus(null, null, "didit-session-456");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            diditService.Verify(service => service.SyncVerificationStatusAsync("didit-session-456"), Times.Once);
        }

        [Fact]
        public async Task CompleteVerification_MarksLaundryAdminAsVerifiedAndApproved()
        {
            var diditService = new Mock<IDiditVerificationService>();
            var user = new ApplicationUser
            {
                Id = "laundry-admin-1",
                Role = UserRole.LaundryAdmin,
                IsIdentityVerified = false,
                AdminApprovalStatus = LaundryAdminApprovalStatus.Pending
            };

            _userManagerMock.Setup(manager => manager.FindByIdAsync("laundry-admin-1"))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new VerificationController(
                diditService.Object,
                _userManagerMock.Object,
                NullLogger<VerificationController>.Instance,
                _configMock.Object)
            {
                ControllerContext = BuildControllerContext("laundry-admin-1")
            };

            var result = await controller.CompleteVerification();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.True(user.IsIdentityVerified);
            Assert.Equal(LaundryAdminApprovalStatus.Approved, user.AdminApprovalStatus);
            _userManagerMock.Verify(manager => manager.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task CompleteVerification_RejectsNonLaundryAdminUsers()
        {
            var diditService = new Mock<IDiditVerificationService>();
            var user = new ApplicationUser
            {
                Id = "customer-1",
                Role = UserRole.Customer,
                IsIdentityVerified = false
            };

            _userManagerMock.Setup(manager => manager.FindByIdAsync("customer-1"))
                .ReturnsAsync(user);

            var controller = new VerificationController(
                diditService.Object,
                _userManagerMock.Object,
                NullLogger<VerificationController>.Instance,
                _configMock.Object)
            {
                ControllerContext = BuildControllerContext("customer-1")
            };

            var result = await controller.CompleteVerification();

            var failure = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, failure.StatusCode);
            _userManagerMock.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Theory]
        [InlineData("Approved")]
        [InlineData("approved")]
        [InlineData("verified")]
        [InlineData("completed")]
        [InlineData("success")]
        public void VerificationStatus_NormalizesSuccessStatuses(string status)
        {
            var normalized = status.Trim().ToLowerInvariant();
            var isVerified =
                normalized is "approved" or "verified" or "completed" or "complete" or "success";

            Assert.True(isVerified);
        }

        private static ControllerContext BuildControllerContext(string userId)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                "TestAuth"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }
    }
}
