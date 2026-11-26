using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LaundryDomain.Models.UserModules;
using LaundryServies.Services.Auth;
using LaundryShared.Common;
using LaundryShared.DTO.Auth;
using LaundryShared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LaundryApi.Tests
{
    public class AuthServiceTests
    {
        private static (AuthService svc, Mock<UserManager<ApplicationUser>> umMock, Mock<SignInManager<ApplicationUser>> simMock)
            BuildService()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var umMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var simMock = new Mock<SignInManager<ApplicationUser>>(
                umMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

            var configMock = new Mock<IConfiguration>();
            var jwtSectionMock = new Mock<IConfigurationSection>();
            jwtSectionMock.Setup(s => s["Key"]).Returns("super_secret_jwt_key_that_is_long_enough_for_sha256_1234567890!");
            jwtSectionMock.Setup(s => s["DurationInDays"]).Returns("7");
            jwtSectionMock.Setup(s => s["Issuer"]).Returns("Nadeef");
            jwtSectionMock.Setup(s => s["Audience"]).Returns("NadeefClients");
            configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);

            var svc = new AuthService(umMock.Object, simMock.Object, configMock.Object);
            return (svc, umMock, simMock);
        }

        [Fact]
        public async Task LoginAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var (svc, umMock, _) = BuildService();
            umMock.Setup(um => um.FindByEmailAsync("nonexistent@test.com"))
                  .ReturnsAsync((ApplicationUser)null!);

            var dto = new LoginDto { Email = "nonexistent@test.com", Password = "Password123!" };

            // Act
            var result = await svc.LoginAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid Email or Account is suspended.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_ReturnsInvalidPassword_WhenCheckPasswordSignInFails()
        {
            // Arrange
            var (svc, umMock, simMock) = BuildService();
            var user = new ApplicationUser { Id = "sa-1", Email = "superadmin@ndeef.com", IsActive = true };
            umMock.Setup(um => um.FindByEmailAsync("superadmin@ndeef.com"))
                  .ReturnsAsync(user);

            simMock.Setup(sim => sim.CheckPasswordSignInAsync(user, "WrongPassword", false))
                   .ReturnsAsync(SignInResult.Failed);

            var dto = new LoginDto { Email = "superadmin@ndeef.com", Password = "WrongPassword" };

            // Act
            var result = await svc.LoginAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid Password.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_ReturnsPleaseVerifyEmail_WhenSuperAdminEmailNotVerified()
        {
            // Arrange
            var (svc, umMock, simMock) = BuildService();
            var user = new ApplicationUser 
            { 
                Id = "sa-1", 
                Email = "superadmin@ndeef.com", 
                IsActive = true,
                IsEmailVerified = false,
                Role = UserRole.SuperAdmin
            };
            umMock.Setup(um => um.FindByEmailAsync("superadmin@ndeef.com"))
                  .ReturnsAsync(user);

            simMock.Setup(sim => sim.CheckPasswordSignInAsync(user, "Basel@2004", false))
                   .ReturnsAsync(SignInResult.Success);

            var dto = new LoginDto { Email = "superadmin@ndeef.com", Password = "Basel@2004" };

            // Act
            var result = await svc.LoginAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Please verify your email first.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenSuperAdminLogsInSuccessfully()
        {
            // Arrange
            var (svc, umMock, simMock) = BuildService();
            var user = new ApplicationUser 
            { 
                Id = "sa-1", 
                Email = "superadmin@ndeef.com", 
                Name = "Super Admin User",
                IsActive = true,
                IsEmailVerified = true,
                Role = UserRole.SuperAdmin
            };
            umMock.Setup(um => um.FindByEmailAsync("superadmin@ndeef.com"))
                  .ReturnsAsync(user);

            simMock.Setup(sim => sim.CheckPasswordSignInAsync(user, "Basel@2004", false))
                   .ReturnsAsync(SignInResult.Success);

            var dto = new LoginDto { Email = "superadmin@ndeef.com", Password = "Basel@2004" };

            // Act
            var result = await svc.LoginAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("sa-1", result.Value.Id);
            Assert.Equal("SuperAdmin", result.Value.Role);
            Assert.Equal("superadmin@ndeef.com", result.Value.Email);
            Assert.Equal("Super Admin User", result.Value.Name);
            Assert.NotNull(result.Value.Token);
        }
    }
}
