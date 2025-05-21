using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Moq.EntityFrameworkCore;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.Services;
using OA.Domain.VModels;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using OA.Service.Helpers;
using Xunit;

namespace MyApp.Tests.Services
{
    public class AspNetUserServiceTests
    {
        private readonly Mock<UserManager<AspNetUser>> _userManagerMock;
        private readonly Mock<RoleManager<AspNetRole>> _roleManagerMock;
        private readonly Mock<IBaseRepository<Department>> _departmentRepoMock;
        private readonly Mock<IBaseRepository<SysFile>> _sysFileRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AspNetUserService _service;

        public AspNetUserServiceTests()
        {
            var userStoreMock = new Mock<IUserStore<AspNetUser>>();
            var roleStoreMock = new Mock<IRoleStore<AspNetRole>>();
            _userManagerMock = new Mock<UserManager<AspNetUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            _roleManagerMock = new Mock<RoleManager<AspNetRole>>(roleStoreMock.Object, null, null, null, null);
            _departmentRepoMock = new Mock<IBaseRepository<Department>>();
            _sysFileRepoMock = new Mock<IBaseRepository<SysFile>>();
            _mapperMock = new Mock<IMapper>();

            _service = new AspNetUserService(
                _roleManagerMock.Object,
                _userManagerMock.Object,
                configService: null,
                jwtFactory: null,
                authMessageSender: null,
                contextAccessor: null,
                mapper: _mapperMock.Object,
                sysFileRepo: _sysFileRepoMock.Object,
                departmentService: _departmentRepoMock.Object,
                dbContext: null
            );
        }

        [Fact]
        public async Task CheckValidUserName_ShouldThrow_WhenExists()
        {
            // Arrange
            var userName = "existingUser";
            _userManagerMock.Setup(u => u.FindByNameAsync(userName)).ReturnsAsync(new AspNetUser());

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CheckValidUserName(userName));
        }

        [Fact]
        public async Task CheckValidEmail_ShouldThrow_WhenExists()
        {
            // Arrange
            var email = "test@example.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(new AspNetUser());

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CheckValidEmail(email));
        }

        [Fact]
        public async Task GetById_ShouldReturnMappedUser_WhenExists()
        {
            // Arrange
            var userId = "U001";
            var user = new AspNetUser
            {
                Id = userId,
                FullName = "John Doe",
                AvatarFileId = 1
            };
            var roles = new List<string> { "Admin" };
            var file = new SysFile { Id = 1, Path = "avatar.png" };

            _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _mapperMock.Setup(m => m.Map<AspNetUser, UserGetByIdVModel>(user)).Returns(new UserGetByIdVModel { Id = user.Id });
            _sysFileRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(file);

            // Act
            var result = await _service.GetById(userId);

            // Assert
            var model = Assert.IsType<UserGetByIdVModel>(result.Data);
            Assert.Equal(userId, model.Id);
        }

        [Fact]
        public async Task UpdateJsonUserHasFunctions_ShouldUpdate_WhenValid()
        {
            // Arrange
            var user = new AspNetUser { Id = "U001" };
            var model = new UpdatePermissionVModel
            {
                UserId = user.Id,
                JsonUserHasFunctions = "{\"a\":1}"
            };

            _userManagerMock.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.UpdateJsonUserHasFunctions(model);

            // Assert
            _userManagerMock.Verify(u => u.UpdateAsync(It.Is<AspNetUser>(x => x.JsonUserHasFunctions == model.JsonUserHasFunctions)), Times.Once);
        }

        [Fact]
        public async Task ChangeStatus_ShouldToggleActive_WhenUserFound()
        {
            // Arrange
            var user = new AspNetUser { Id = "U001", IsActive = true };
            _userManagerMock.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.ChangeStatus(user.Id);

            // Assert
            Assert.False(user.IsActive);
        }
    }
}
