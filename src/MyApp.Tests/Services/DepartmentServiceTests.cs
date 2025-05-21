using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using OA.Service.Helpers;
using Xunit;

namespace MyApp.Tests.Services
{
    public class DepartmentServiceTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IBaseRepository<Department>> _departmentRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly DepartmentService _service;

        public DepartmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _departmentRepoMock = new Mock<IBaseRepository<Department>>();
            _mapperMock = new Mock<IMapper>();

            _service = new DepartmentService(_dbContext, _departmentRepoMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task Create_ShouldCallRepositoryCreate_WhenValidModel()
        {
            // Arrange
            var model = new DepartmentCreateVModel { Name = "IT" };
            var entity = new Department { Name = "IT" };

            _mapperMock.Setup(m => m.Map<DepartmentCreateVModel, Department>(model)).Returns(entity);
            _departmentRepoMock.Setup(r => r.Create(entity)).ReturnsAsync(new ResponseResult { Success = true });

            // Act
            await _service.Create(model);

            // Assert
            _departmentRepoMock.Verify(r => r.Create(It.IsAny<Department>()), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldCallRepositoryUpdate_WhenValidModel()
        {
            // Arrange
            var model = new DepartmentUpdateVModel { Id = 1, Name = "HR" };
            var entity = new Department { Id = 1, Name = "HR" };

            _mapperMock.Setup(m => m.Map<DepartmentUpdateVModel, Department>(model)).Returns(entity);
            _departmentRepoMock.Setup(r => r.Update(entity)).ReturnsAsync(new ResponseResult { Success = true });

            // Act
            await _service.Update(model);

            // Assert
            _departmentRepoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnCorrectCountAndFilter()
        {
            // Arrange
            _dbContext.Department.Add(new Department
            {
                Id = 1,
                Name = "Finance",
                CreatedDate = DateTime.Today,
                IsActive = true,
                DepartmentHeadId = "U001"
            });

            _dbContext.AspNetUsers.Add(new AspNetUser
            {
                Id = "U001",
                DepartmentId = 1,
                IsActive = true,
                FullName = "Nguyen Van A",
                EmployeeId = "EMP001"
            });

            await _dbContext.SaveChangesAsync();

            var model = new DepartmentFilterVModel
            {
                PageNumber = 1,
                PageSize = 10,
                IsDescending = false,
                Keyword = "finance",
                CreatedDate = DateTime.Today
            };

            _mapperMock.Setup(m => m.Map<DepartmentGetAllVModel>(It.IsAny<Department>()))
                .Returns((Department dept) => new DepartmentGetAllVModel
                {
                    Id = dept.Id,
                    Name = dept.Name,
                    CreatedDate = dept.CreatedDate
                });

            // Act
            var result = await _service.Search(model);

            // Assert
            var paged = (Pagination)result.Data;
            Assert.Single(paged.Records);
            var record = paged.Records.Cast<DepartmentGetAllVModel>().First();
            Assert.Equal("Finance", record.Name);
        }
    }
}
