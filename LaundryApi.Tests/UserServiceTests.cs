using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaundryDomain.IRepositories;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryServies.Common;
using LaundryServies.Services.UserModule;
using LaundryShared.Common;
using LaundryShared.DTO.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LaundryApi.Tests
{
    public class UserServiceTests
    {
        private static NadeefDbContext BuildDb()
        {
            var opts = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new NadeefDbContext(opts);
        }

        private static (UserService svc, Mock<UserManager<ApplicationUser>> umMock, Mock<IFileService> fileServiceMock)
            BuildService(NadeefDbContext ctx)
        {
            var uow = new LaundryPerstince.Data.Repositories.UnitOfWork(ctx);
            var store = new Mock<IUserStore<ApplicationUser>>();
            var umMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var fileServiceMock = new Mock<IFileService>();

            var svc = new UserService(uow, umMock.Object, fileServiceMock.Object);
            return (svc, umMock, fileServiceMock);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            using var ctx = BuildDb();
            var (svc, umMock, _) = BuildService(ctx);
            
            umMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                  .ReturnsAsync((ApplicationUser)null!);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await svc.ChangePasswordAsync("nonexistent-id", request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsCurrentPasswordIncorrect_WhenCheckPasswordFails()
        {
            // Arrange
            using var ctx = BuildDb();
            var (svc, umMock, _) = BuildService(ctx);
            
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com" };
            umMock.Setup(um => um.FindByIdAsync("user-1"))
                  .ReturnsAsync(user);

            umMock.Setup(um => um.CheckPasswordAsync(user, "WrongPassword"))
                  .ReturnsAsync(false);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await svc.ChangePasswordAsync("user-1", request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Current password is not correct.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsFailure_WhenRemovePasswordFails()
        {
            // Arrange
            using var ctx = BuildDb();
            var (svc, umMock, _) = BuildService(ctx);
            
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com" };
            umMock.Setup(um => um.FindByIdAsync("user-1"))
                  .ReturnsAsync(user);

            umMock.Setup(um => um.CheckPasswordAsync(user, "OldPassword123!"))
                  .ReturnsAsync(true);

            umMock.Setup(um => um.RemovePasswordAsync(user))
                  .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error removing password." }));

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await svc.ChangePasswordAsync("user-1", request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Failed to process password update.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsFailure_WhenAddPasswordFails()
        {
            // Arrange
            using var ctx = BuildDb();
            var (svc, umMock, _) = BuildService(ctx);
            
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com" };
            umMock.Setup(um => um.FindByIdAsync("user-1"))
                  .ReturnsAsync(user);

            umMock.Setup(um => um.CheckPasswordAsync(user, "OldPassword123!"))
                  .ReturnsAsync(true);

            umMock.Setup(um => um.RemovePasswordAsync(user))
                  .ReturnsAsync(IdentityResult.Success);

            umMock.Setup(um => um.AddPasswordAsync(user, "NewPassword123!"))
                  .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too short." }));

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await svc.ChangePasswordAsync("user-1", request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Password too short.", result.ErrorMessage);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsSuccess_WhenPasswordIsUpdatedSuccessfully()
        {
            // Arrange
            using var ctx = BuildDb();
            var (svc, umMock, _) = BuildService(ctx);
            
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com" };
            umMock.Setup(um => um.FindByIdAsync("user-1"))
                  .ReturnsAsync(user);

            umMock.Setup(um => um.CheckPasswordAsync(user, "OldPassword123!"))
                  .ReturnsAsync(true);

            umMock.Setup(um => um.RemovePasswordAsync(user))
                  .ReturnsAsync(IdentityResult.Success);

            umMock.Setup(um => um.AddPasswordAsync(user, "NewPassword123!"))
                  .ReturnsAsync(IdentityResult.Success);

            umMock.Setup(um => um.UpdateSecurityStampAsync(user))
                  .ReturnsAsync(IdentityResult.Success);

            umMock.Setup(um => um.UpdateAsync(user))
                  .ReturnsAsync(IdentityResult.Success);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await svc.ChangePasswordAsync("user-1", request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            
            umMock.Verify(um => um.CheckPasswordAsync(user, "OldPassword123!"), Times.Once);
            umMock.Verify(um => um.RemovePasswordAsync(user), Times.Once);
            umMock.Verify(um => um.AddPasswordAsync(user, "NewPassword123!"), Times.Once);
            umMock.Verify(um => um.UpdateSecurityStampAsync(user), Times.Once);
            umMock.Verify(um => um.UpdateAsync(user), Times.Once);
        }
    }
}
