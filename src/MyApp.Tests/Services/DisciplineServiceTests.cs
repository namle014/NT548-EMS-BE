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

public class DisciplineServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly DisciplineService _service;
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IBaseRepository<Discipline>> _disciplineRepo = new();
    private readonly Mock<IBaseRepository<SysFile>> _sysFileRepo = new();
    private readonly Mock<UserManager<AspNetUser>> _userManager = new(Mock.Of<IUserStore<AspNetUser>>(), null, null, null, null, null, null, null, null);

    public DisciplineServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        _context = new ApplicationDbContext(options);

        _service = new DisciplineService(
            _context,
            _userManager.Object,
            _disciplineRepo.Object,
            _mapper.Object,
            _sysFileRepo.Object
        );
    }


    [Fact]
    public async Task UpdateIsPenalized_ShouldToggleFlag()
    {
        var discipline = new Discipline { Id = 2, IsPenalized = false };
        _disciplineRepo.Setup(x => x.GetById(2)).ReturnsAsync(discipline);
        _disciplineRepo.Setup(x => x.Update(discipline)).ReturnsAsync(new ResponseResult { Success = true });

        await _service.UpdateIsPenalized(new UpdateIsPenalizedVModel { Id = 2 });

        Assert.True(discipline.IsPenalized);
    }

    [Fact]
    public async Task GetTotalDisciplines_ShouldReturnStats()
    {
        _context.Discipline.Add(new Discipline
        {
            Date = new DateTime(2024, 5, 10),
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetTotalDisciplines(2024, 5);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetDisciplineStatInYear_ShouldReturnMonthlyStats()
    {
        _context.Discipline.Add(new Discipline
        {
            Date = new DateTime(2024, 1, 15),
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetDisciplineStatInYear(2024);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetTotalDisciplineByEmployeeInMonth_ShouldReturnStats()
    {
        // Arrange
        _context.Reward.Add(new Reward
        {
            Id = 1,
            UserId = "user1",
            Date = new DateTime(2024, 5, 1)
        });
        _context.AspNetUsers.Add(new AspNetUser
        {
            Id = "user1",
            FullName = "Nguyen Van A"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetTotalDisciplineByEmployeeInMonth(2024, 5);
        Assert.NotNull(result.Data);
    }
}
