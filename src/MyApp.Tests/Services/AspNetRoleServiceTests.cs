using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using OA.Core.Models;
using OA.Core.VModels;
using OA.Domain.VModels.Role;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using OA.Service.Helpers;
using OA.Core.Repositories;
using Microsoft.EntityFrameworkCore.InMemory;

namespace MyApp.Tests.Services
{
    public class AspNetRoleServiceTests
    {
        private readonly Mock<RoleManager<AspNetRole>> _roleManagerMock;
        private readonly Mock<UserManager<AspNetUser>> _userManagerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IBaseRepository<SysFunction>> _sysFunctionRepoMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly AspNetRoleService _service;

        public AspNetRoleServiceTests()
        {
            // In-memory DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // Mock dependencies
            var roleStore = new Mock<IRoleStore<AspNetRole>>();
            _roleManagerMock = new Mock<RoleManager<AspNetRole>>(roleStore.Object, null, null, null, null);

            var userStore = new Mock<IUserStore<AspNetUser>>();
            _userManagerMock = new Mock<UserManager<AspNetUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sysFunctionRepoMock = new Mock<IBaseRepository<SysFunction>>();

            _service = new AspNetRoleService(
                _dbContext,
                _roleManagerMock.Object,
                _userManagerMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object,
                _sysFunctionRepoMock.Object
            );
        }

        [Fact]
        public async Task GetById_ShouldReturnRole_WhenExists()
        {
            // Arrange
            var roleId = "R000134";
            var role = new AspNetRole { Id = roleId, Name = "Admin" };
            var mapped = new AspNetRoleGetByIdVModel { Id = roleId, Name = "Admin" };

            _roleManagerMock.Setup(r => r.FindByIdAsync(roleId)).ReturnsAsync(role);
            _mapperMock.Setup(m => m.Map<AspNetRoleGetByIdVModel>(role)).Returns(mapped);

            // Act
            var result = await _service.GetById(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId, ((AspNetRoleGetByIdVModel)result.Data).Id);
        }

        [Fact]
        public async Task CheckValidRoleName_ShouldThrow_WhenExists()
        {
            // Arrange
            var roleName = "Admin";
            _roleManagerMock.Setup(r => r.FindByNameAsync(roleName)).ReturnsAsync(new AspNetRole());

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CheckValidRoleName(roleName));
        }

        [Fact]
        public async Task Create_ShouldThrow_WhenRoleExists()
        {
            // Arrange
            var model = new AspNetRoleCreateVModel { Name = "Admin" };
            _roleManagerMock.Setup(r => r.RoleExistsAsync(model.Name)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _service.Create(model));
        }
    }
}
