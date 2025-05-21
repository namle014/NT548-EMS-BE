using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.VModels;
using OA.Domain.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class TimeOffServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly TimeOffService _service;
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IBaseRepository<SysFile>> _sysFileRepo = new();
    private readonly Mock<IBaseRepository<Department>> _departmentRepo = new();
    private readonly Mock<UserManager<AspNetUser>> _userManager = new(
        Mock.Of<IUserStore<AspNetUser>>(), null, null, null, null, null, null, null, null
    );
    private readonly Mock<RoleManager<AspNetRole>> _roleManager = new(
        Mock.Of<IRoleStore<AspNetRole>>(), null, null, null, null
    );

    public TimeOffServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // chạy ảo
            .Options;

        _context = new ApplicationDbContext(options);

        _service = new TimeOffService(
            _httpContextAccessor.Object,
            _departmentRepo.Object,
            _sysFileRepo.Object,
            _roleManager.Object,
            _userManager.Object,
            _context,
            _mapper.Object
        );
    }

    [Fact]
    public async Task Create_ShouldAddNewTimeOff()
    {
        var model = new TimeOffCreateVModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2),
            Reason = "Vacation",
            Content = "Relax",
            UserId = Guid.NewGuid().ToString(),
        };

        _mapper.Setup(x => x.Map<TimeOffCreateVModel, TimeOff>(model))
            .Returns(new TimeOff { StartDate = model.StartDate, EndDate = model.EndDate, Reason = model.Reason, UserId = model.UserId });

        await _service.Create(model);

        var result = await _context.TimeOff.FirstOrDefaultAsync();
        Assert.NotNull(result);
        Assert.Equal("Vacation", result.Reason);
    }

    [Fact]
    public async Task GetTimeOffIsAccepted_ShouldReturn12Months()
    {
        _context.TimeOff.Add(new TimeOff
        {
            CreatedDate = new DateTime(DateTime.Now.Year, 1, 5),
            IsAccepted = null
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetTimeOffIsAccepted(DateTime.Now.Year);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CountTimeOffsInMonth_ShouldReturnCorrectData()
    {
        _context.TimeOff.Add(new TimeOff { StartDate = new DateTime(2024, 5, 1) });
        await _context.SaveChangesAsync();

        var result = await _service.CountTimeOffsInMonth(2024, 5);
        Assert.NotNull(result.Data);
    }

    //[Fact]
    //public async Task Update_ShouldModifyEntity()
    //{
    //    // Arrange: thêm entity gốc
    //    var originalEntity = new TimeOff
    //    {
    //        Id = 11,
    //        Reason = "Old",
    //        StartDate = DateTime.Today,
    //        EndDate = DateTime.Today.AddDays(1),
    //        UserId = Guid.NewGuid().ToString()
    //    };
    //    await _context.TimeOff.AddAsync(originalEntity);
    //    await _context.SaveChangesAsync();

    //    // Detach để tránh conflict Entity Tracking
    //    _context.Entry(originalEntity).State = EntityState.Detached;

    //    // Act: tạo model mới để cập nhật
    //    var updateModel = new TimeOffUpdateVModel
    //    {
    //        Id = 11,
    //        Reason = "Updated"
    //    };

    //    var updatedEntity = new TimeOff
    //    {
    //        Id = 11,
    //        Reason = "Updated",
    //        StartDate = originalEntity.StartDate,
    //        EndDate = originalEntity.EndDate,
    //        UserId = originalEntity.UserId
    //    };

    //    _mapper.Setup(m => m.Map(updateModel, It.IsAny<TimeOff>())).Returns(updatedEntity);

    //    // Gọi hàm update
    //    await _service.Update(updateModel);

    //    // Assert
    //    var updated = await _context.TimeOff.FindAsync(11);
    //    Assert.Equal("Updated", updated.Reason);
    //}

    [Fact]
    public async Task ChangeStatus_ShouldToggleIsActive()
    {
        var entity = new TimeOff { Id = 20, IsActive = true };
        _context.TimeOff.Add(entity);
        await _context.SaveChangesAsync();

        await _service.ChangeStatus(20);

        var result = await _context.TimeOff.FindAsync(20);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Remove_ShouldDeleteEntity()
    {
        var entity = new TimeOff { Id = 30 };
        _context.TimeOff.Add(entity);
        await _context.SaveChangesAsync();

        await _service.Remove(30);

        var result = await _context.TimeOff.FindAsync(30);
        Assert.Null(result);
    }
}
