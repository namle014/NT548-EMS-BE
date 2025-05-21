using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using OA.Core.Models;
using OA.Core.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using OA.Service.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyApp.Tests.Services
{
    public class EventServiceTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly EventService _service;

        public EventServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var mapperMock = new Mock<IMapper>();
            _mapper = mapperMock.Object;

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _service = new EventService(_dbContext, _mapper, httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Create_ShouldAddEvent_WhenValid()
        {
            // Arrange
            var model = new EventCreateVModel { Title = "Test Event", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            var mappedEntity = new Events { Title = model.Title, StartDate = model.StartDate, EndDate = model.EndDate };

            var mapperMock = Mock.Get(_mapper);
            mapperMock.Setup(m => m.Map<EventCreateVModel, Events>(model)).Returns(mappedEntity);

            // Act
            await _service.Create(model);

            // Assert
            var events = _dbContext.Set<Events>().ToList();
            Assert.Single(events);
            Assert.Equal("Test Event", events.First().Title);
        }

        [Fact]
        public async Task GetById_ShouldReturnEvent_WhenExists()
        {
            // Arrange
            var ev = new Events { Title = "Test Event", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            _dbContext.Set<Events>().Add(ev);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetById(ev.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ev.Id, ((Events)result.Data).Id);
        }

        [Fact]
        public async Task Remove_ShouldDeleteEvent_WhenExists()
        {
            // Arrange
            var ev = new Events { Title = "To Remove", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            _dbContext.Set<Events>().Add(ev);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.Remove(ev.Id);

            // Assert
            var found = await _dbContext.Set<Events>().FindAsync(ev.Id);
            Assert.Null(found);
        }

        [Fact]
        public async Task Update_ShouldModifyEvent_WhenExists()
        {
            // Arrange
            var ev = new Events { Title = "Before Update", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            _dbContext.Set<Events>().Add(ev);
            await _dbContext.SaveChangesAsync();

            var model = new EventUpdateVModel { Id = ev.Id, Title = "After Update" };
            var mapperMock = Mock.Get(_mapper);
            mapperMock.Setup(m => m.Map(model, ev)).Callback(() => ev.Title = model.Title);

            // Act
            await _service.Update(model);

            // Assert
            var updated = await _dbContext.Set<Events>().FindAsync(ev.Id);
            Assert.Equal("After Update", updated.Title);
        }
    }
}