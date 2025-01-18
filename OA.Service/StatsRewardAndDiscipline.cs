using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OA.Core.Models;
using OA.Core.Services;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OA.Service
{
    public class StatsRewardAndDiscipline : IStatsRewardAndDiscipline
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private DbSet<Reward> _reward;
        private DbSet<SysFile> _sysFiles;
        private DbSet<Discipline> _discipline;
        private readonly UserManager<AspNetUser> _userManager;

        public StatsRewardAndDiscipline(ApplicationDbContext dbContext, IMapper mapper, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _reward = dbContext.Set<Reward>();
            _discipline = dbContext.Set<Discipline>();
            _sysFiles = dbContext.Set<SysFile>();
            _userManager = userManager;
        }

        public async Task<ResponseResult> StatsDisplay(int month, int year)
        {
            var result = new ResponseResult();

            var reward = _reward.AsQueryable();
            var discipline = _discipline.AsQueryable();

            var currentReward = await reward.Where(x => x.Date.Month == month && x.Date.Year == year).CountAsync();
            var currentDiscipline = await discipline.Where(x => x.Date.Month == month && x.Date.Year == year).CountAsync();

            if (month == 1)
            {
                month = 12;
                year--;
            }
            else
            {
                month--;
            }

            var prevReward = await reward.Where(x => x.Date.Month == month && x.Date.Year == year).CountAsync();
            var prevDiscipline = await discipline.Where(x => x.Date.Month == month && x.Date.Year == year).CountAsync();

            double rewardPercent = 100.0;
            if (prevReward != 0)
            {
                rewardPercent = 1.0 * (currentReward - prevReward) / prevReward * 100.0;
            }

            double disciplinePercent = 100.0;
            if (prevDiscipline != 0)
            {
                disciplinePercent = 1.0 * (currentDiscipline - prevDiscipline) / prevDiscipline * 100.0;
            }

            result.Data = new
            {
                currentReward = currentReward,
                currentDiscipline = currentDiscipline,
                rewardPercent = Math.Round(rewardPercent, 2),
                disciplinePercent = Math.Round(disciplinePercent, 2)
            };

            return result;
        }

        public async Task<ResponseResult> StatsChart(int year)
        {
            var result = new ResponseResult();

            var reward = _reward.AsQueryable();
            var discipline = _discipline.AsQueryable();

            var resultReward = new List<int>();
            var resultDiscipline = new List<int>();

            int sumReward = 0;
            int sumDiscipline = 0;

            for (int i = 1; i <= 12; i++)
            {
                var currentReward = await reward.Where(x => x.Date.Month == i && x.Date.Year == year).CountAsync();
                var currentDiscipline = await discipline.Where(x => x.Date.Month == i && x.Date.Year == year).CountAsync();

                resultReward.Add(currentReward);
                resultDiscipline.Add(currentDiscipline);

                sumReward += currentReward;
                sumDiscipline += currentDiscipline;
            }

            result.Data = new
            {
                ListReward = resultReward,
                ListDiscipline = resultDiscipline,
                Percent = Math.Round(1.0 * sumReward / (sumReward + sumDiscipline) * 100, 2)
            };

            return result;
        }

        public async Task<ResponseResult> TopUserByMonth(int month, int year)
        {
            var result = new ResponseResult();

            // Truy vấn danh sách thưởng, sắp xếp giảm dần theo Count
            var topRewards = await (from reward in _reward.AsQueryable()
                                    join user in _userManager.Users.AsQueryable()
                                    on reward.UserId equals user.Id
                                    join file in _sysFiles.AsQueryable()
                                    on user.AvatarFileId equals file.Id into userFiles
                                    from file in userFiles.DefaultIfEmpty()  // LEFT JOIN
                                    where reward.Date.Month == month && reward.Date.Year == year
                                    group reward by new
                                    {
                                        user.EmployeeId,
                                        user.FullName,
                                        AvatarPath = file != null && file.Path != null ? "https://localhost:44381/" + file.Path : null
                                    } into g
                                    orderby g.Count() descending
                                    select new
                                    {
                                        FullName = g.Key.FullName,
                                        EmployeeId = g.Key.EmployeeId,
                                        AvatarPath = g.Key.AvatarPath,
                                        Count = g.Count()
                                    }).ToListAsync();


            // Truy vấn danh sách kỷ luật, sắp xếp giảm dần theo Count
            var topDisciplines = await (from discipline in _discipline.AsQueryable()
                                        join user in _userManager.Users.AsQueryable()
                                        on discipline.UserId equals user.Id
                                        join file in _sysFiles.AsQueryable()
                                        on user.AvatarFileId equals file.Id into userFiles
                                        from file in userFiles.DefaultIfEmpty()
                                        where discipline.Date.Month == month && discipline.Date.Year == year
                                        group discipline by new
                                        {
                                            user.EmployeeId,
                                            user.FullName,
                                            AvatarPath = file != null && file.Path != null ? "https://localhost:44381/" + file.Path : null
                                        } into g
                                        orderby g.Count() descending
                                        select new
                                        {
                                            FullName = g.Key.FullName,
                                            EmployeeId = g.Key.EmployeeId,
                                            AvatarPath = g.Key.AvatarPath,
                                            Count = g.Count()
                                        }).ToListAsync();


            // Đưa vào kết quả trả về
            result.Data = new
            {
                TopRewards = topRewards,
                TopDisciplines = topDisciplines,
            };

            return result;
        }
    }
}
