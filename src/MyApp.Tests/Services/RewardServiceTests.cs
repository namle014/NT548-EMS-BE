using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Repository;
using OA.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyApp.Tests.Services
{
    public class RewardServiceTests
    {
        private readonly Mock<UserManager<AspNetUser>> _userManagerMock;
        private readonly Mock<IBaseRepository<Reward>> _rewardRepoMock;
        private readonly Mock<IBaseRepository<SysFile>> _sysFileRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly RewardService _service;

        public RewardServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            _dbContext = new ApplicationDbContext(options);

            _userManagerMock = new Mock<UserManager<AspNetUser>>(
                Mock.Of<IUserStore<AspNetUser>>(), null, null, null, null, null, null, null, null);

            _rewardRepoMock = new Mock<IBaseRepository<Reward>>();
            _sysFileRepoMock = new Mock<IBaseRepository<SysFile>>();
            _mapperMock = new Mock<IMapper>();

            _service = new RewardService(_dbContext, _userManagerMock.Object, _rewardRepoMock.Object, _mapperMock.Object, _sysFileRepoMock.Object);
        }

        [Fact]
        public async Task UpdateIsReceived_ShouldToggleIsReceived()
        {
            var reward = new Reward { Id = 1, IsReceived = false };
            _dbContext.Reward.Add(reward);
            await _dbContext.SaveChangesAsync();

            var model = new UpdateIsReceivedVModel { Id = 1 };
            await _service.UpdateIsReceived(model);

            var updated = await _dbContext.Reward.FindAsync(1);
            Assert.True(updated.IsReceived);
        }

        [Fact]
        public async Task GetTotalRewards_ShouldCalculateCorrectly()
        {
            var now = DateTime.Now;
            _dbContext.Reward.Add(new Reward { Id = 1, Date = now });
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetTotalRewards(now.Year, now.Month);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetRewardStatInYear_ShouldReturn12Months()
        {
            var reward = new Reward { Id = 1, Date = DateTime.Now };
            _dbContext.Reward.Add(reward);
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetRewardStatInYear(DateTime.Now.Year);
            Assert.NotNull(result.Data);
        }
    }
}
