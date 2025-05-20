using Xunit;
using Moq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using OA.Core.VModels;
using OA.Core.Models;
using Ganss.Xss;
using OA.Repository;
using Employee_Management_System.Hubs;
using OA.Core.Repositories;

namespace OA.Tests.Services
{
    public class NotificationsServiceTests
    {
        private readonly Mock<UserManager<AspNetUser>> _userManagerMock;
        private readonly Mock<RoleManager<AspNetRole>> _roleManagerMock;
        private readonly Mock<IBaseRepository<Department>> _deptRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly HtmlSanitizer _sanitizer;

        private readonly NotificationsService _service;

        public NotificationsServiceTests()
        {
            var userStore = new Mock<IUserStore<AspNetUser>>();
            _userManagerMock = new Mock<UserManager<AspNetUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<AspNetRole>>();
            _roleManagerMock = new Mock<RoleManager<AspNetRole>>(roleStore.Object, null, null, null, null);

            _deptRepoMock = new Mock<IBaseRepository<Department>>();
            _mapperMock = new Mock<IMapper>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _sanitizer = new HtmlSanitizer();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var contextAccessor = new Mock<IHttpContextAccessor>();

            _service = new NotificationsService(
                _dbContext,
                _userManagerMock.Object,
                _sanitizer,
                _roleManagerMock.Object,
                _deptRepoMock.Object,
                _hubContextMock.Object,
                _mapperMock.Object,
                contextAccessor.Object
            );
        }

        [Fact]
        public async Task StatNotificationByMonth_ShouldReturnData()
        {
            // Arrange
            var notification = new Notifications
            {
                Id = 1,
                Title = "Test",
                Content = "Test content",
                SentTime = DateTime.UtcNow.AddHours(7),
                Type = "Public",
                IsActive = true,
                UserId = "U001"
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.StatNotificationByMonth(DateTime.Now.Month, DateTime.Now.Year);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task StatNotificationByType_ShouldReturnTypeCounts()
        {
            // Arrange
            var notification = new Notifications
            {
                Id = 2,
                Title = "Test",
                Content = "Test content",
                SentTime = DateTime.UtcNow.AddHours(7),
                Type = "Event",
                IsActive = true,
                UserId = "U001"
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.StatNotificationByType(DateTime.Now.Year);

            // Assert
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task ChangeStatus_ShouldToggleStatus()
        {
            // Arrange
            var noti = new Notifications { Id = 10, IsActive = true, Title = "test", Content = "test", SentTime = DateTime.Now, Type = "Public", UserId = "U001" };
            _dbContext.Notifications.Add(noti);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.ChangeStatus(noti.Id);

            // Assert
            var updated = await _dbContext.Notifications.FindAsync(noti.Id);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task Remove_ShouldDeleteNotification()
        {
            // Arrange
            var noti = new Notifications { Id = 20, IsActive = true, Title = "test", Content = "test", SentTime = DateTime.Now, Type = "Public", UserId = "U001" };
            _dbContext.Notifications.Add(noti);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.Remove(noti.Id);

            // Assert
            var result = await _dbContext.Notifications.FindAsync(noti.Id);
            Assert.Null(result);
        }
    }
}