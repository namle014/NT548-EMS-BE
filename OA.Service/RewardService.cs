using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OA.Core.Constants;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.VModels;
using OA.Domain.Services;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Repository;
using OA.Service.Helpers;
using System.Diagnostics.Eventing.Reader;
//using Twilio.TwiML.Voice;

namespace OA.Service
{
    public class RewardService : BaseService<Reward, RewardCreateVModel, RewardUpdateVModel, RewardGetByIdVModel, RewardGetAllVModel, RewardExportVModel>, IRewardService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private DbSet<Reward> _reward;
        private DbSet<Department> _dept;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IBaseRepository<SysFile> _sysFileRepo;

        public RewardService(ApplicationDbContext dbContext, UserManager<AspNetUser> userManager,
                            IBaseRepository<Reward> rewardRepo, IMapper mapper, IBaseRepository<SysFile> sysFileRepo) : base(rewardRepo, mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException("context");
            _reward = dbContext.Set<Reward>();
            _dept = dbContext.Set<Department>();
            _userManager = userManager;
            _mapper = mapper;
            _sysFileRepo = sysFileRepo;
        }

        public async Task<ResponseResult> Search(RewardFilterVModel model)
        {
            var result = new ResponseResult();

            // Khởi tạo danh sách các UserId và phòng ban nếu có
            var records = await _reward.Where(x => x.IsActive == true).ToListAsync();
            var userIds = records.Select(x => x.UserId).Distinct().ToList();
            Department? department = null;

            // Tìm phòng ban nếu có trong bộ lọc
            if (!string.IsNullOrEmpty(model.Department))
            {
                department = _dept.Where(x => x.Name == model.Department).FirstOrDefault();
            }

            // Lọc người dùng theo từ khóa (FullName) và phòng ban nếu có
            var usersQuery = await _userManager.Users.Where(x => (model.Keyword == null || x.FullName.ToLower().Contains(model.Keyword.ToLower())) &&
                                                    (department == null || x.DepartmentId == department.Id)).ToListAsync();

            // Lấy danh sách tất cả phòng ban trước
            var allDepartments = _dept.ToList();

            var avatarFileIds = usersQuery.Where(x => x.AvatarFileId.HasValue)
                               .Select(x => x.AvatarFileId)
                               .Distinct()
                               .ToList();

            var avatarFiles = await _dbContext.Set<SysFile>()
                           .Where(x => avatarFileIds.Contains(x.Id))
                           .ToListAsync();

            var list = new List<RewardGetAllVModel>();

            // Duyệt qua các bản ghi và ánh xạ
            foreach (var entity in records)
            {
                var user = usersQuery.FirstOrDefault(x => x.Id == entity.UserId);
                if (user == null) continue;

                var vmodel = _mapper.Map<RewardGetAllVModel>(entity);
                vmodel.FullName = user.FullName;
                vmodel.EmployeeId = user.EmployeeId;

                var avatar = avatarFiles.FirstOrDefault(x => x.Id == user.AvatarFileId);
                vmodel.AvatarPath = avatar != null ? "https://localhost:44381/" + avatar.Path : null;

                int deptId = user.DepartmentId ?? 0;
                var dept = allDepartments.FirstOrDefault(x => x.Id == deptId);
                vmodel.Department = dept?.Name;

                list.Add(vmodel);
            }

            // Sắp xếp kết quả
            list = string.IsNullOrEmpty(model.SortBy)
                ? (model.IsDescending ? list.OrderByDescending(r => r.Id) : list.OrderBy(r => r.Id)).ToList()
                : (model.IsDescending
                    ? list.OrderByDescending(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null))
                    : list.OrderBy(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null)))
                .ToList();

            // Phân trang kết quả
            var pagedRecords = list.Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToList();

            result.Data = new Pagination
            {
                Records = pagedRecords,
                TotalRecords = list.Count
            };

            return result;
        }

        public async Task<ExportStream> ExportFile(RewardFilterVModel model, ExportFileVModel exportModel)
        {
            model.IsExport = true;
            var result = await Search(model);

            var records = _mapper.Map<IEnumerable<RewardExportVModel>>(result.Data?.Records);
            var exportData = ImportExportHelper<RewardExportVModel>.ExportFile(exportModel, records);
            return exportData;
        }
        public override async Task Create(RewardCreateVModel model)
        {
            var rewardCreate = _mapper.Map<RewardCreateVModel, Reward>(model);
            rewardCreate.Date = DateTime.Now;

            await base.Create(model);
        }

        public async Task<ResponseResult> GetTotalRewards(int years, int month)
        {
            var result = new ResponseResult();

            var previousMonth = month == 1 ? 12 : month - 1;
            var previousYear = month == 1 ? years - 1 : years;

            var firstDayOfMonth = new DateTime(years, month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var firstDayOfPreviousMonth = new DateTime(previousYear, previousMonth, 1);
            var lastDayOfPreviousMonth = firstDayOfPreviousMonth.AddMonths(1).AddDays(-1);

            var rewards = await _reward.Where(c => c.Date <= lastDayOfMonth && c.Date >= firstDayOfPreviousMonth).ToListAsync();

            var rewardsInMonth = rewards.Count(c =>
                c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth);

            var rewardInPreviousMonth = rewards.Count(c =>
                c.Date <= lastDayOfPreviousMonth && c.Date >= firstDayOfPreviousMonth);

            var rewardpercent = 0;
            if (rewardInPreviousMonth == 0)
            {
                rewardpercent = 100;
            }
            else
            {
                rewardpercent = (rewardsInMonth - rewardInPreviousMonth) * 100 / rewardInPreviousMonth;
            }


            result.Data = new
            {
                TotalBenefit = rewardsInMonth,
                BenefitPercent = rewardpercent,
            };
            return result;
        }

        public Task<ResponseResult> GetTotalRewardByEmployeeInMonth(int year, int month)
        {
            var result = new ResponseResult();

            try
            {
                var rewardStats = new List<object>();

                var firstDayOfMonth = new DateTime(year, month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var rewards = _reward
                        .Where(r => r.Date >= firstDayOfMonth && r.Date <= lastDayOfMonth)
                        .GroupBy(r => r.UserId)
                        .Select(g => new
                        {
                            UserId = g.Key,
                            RewardCount = g.Count(),
                            FullName = _userManager.Users
                                .Where(u => u.Id == g.Key)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                        })
                        .OrderByDescending(r => r.RewardCount) // Sắp xếp giảm dần
                        .ToList();
                foreach (var reward in rewards)
                {
                    rewardStats.Add(new
                    {
                        FullNames = reward.FullName,
                        TotalRewards = reward.RewardCount
                    });
                }

                result.Data = new
                {
                    DataRewards = rewardStats
                };

            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return Task.FromResult(result); // Bọc result trong Task
        }

        public async Task<ResponseResult> GetRewardStatInYear(int year)
        {
            var result = new ResponseResult();
            try
            {
                var yearStats = new List<object>();

                for (int month = 1; month <= 12; month++)
                {

                    var firstDayOfMonth = new DateTime(year, month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);



                    var rewards = await _reward.Where(c => c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth).ToListAsync();

                    var rewardsInMonth = rewards.Count(c =>
                        c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth);




                    yearStats.Add(new
                    {
                        Month = month,
                        RewardsInMonths = rewardsInMonth,
                    });
                }

                result.Data = new
                {
                    Year = year,
                    MonthlyStats = yearStats
                };
            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return result;
        }
        public async Task<ResponseResult> GetMeRewardInfo(RewardFilterVModel model, int year)
        {
            var result = new ResponseResult();
            try
            {
                var userId = GlobalVariables.GlobalUserId != null ? GlobalVariables.GlobalUserId : string.Empty;
                var rewardList = await _reward.Where(x => x.IsActive && x.UserId == userId && x.Date.Year == year).ToListAsync();
                string? keyword = model.Keyword?.ToLower();
                var salaryAns = rewardList.Where(x =>
                            (x.IsActive == model.IsActive));
                if (model.IsDescending == false)
                {
                    salaryAns = string.IsNullOrEmpty(model.SortBy)
                            ? salaryAns.OrderBy(r => r.Date).ToList()
                            : salaryAns.OrderBy(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null)).ToList();
                }
                else
                {
                    salaryAns = string.IsNullOrEmpty(model.SortBy)
                            ? salaryAns.OrderByDescending(r => r.Date).ToList()
                            : salaryAns.OrderByDescending(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null)).ToList();
                }

                result.Data = new Pagination();

                var pagedRecords = salaryAns.Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToList();

                result.Data.Records = pagedRecords;
                result.Data.TotalRecords = salaryAns.Count();
            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return result;
        }
    }
}
