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
//using Twilio.TwiML.Voice;

namespace OA.Service
{
    public class DisciplineService : BaseService<Discipline, DisciplineCreateVModel, DisciplineUpdateVModel, DisciplineGetByIdVModel, DisciplineGetAllVModel, DisciplineExportVModel>, IDisciplineService
    {
        private readonly IBaseRepository<Discipline> _disciplineRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private DbSet<Discipline> _discipline;
        private DbSet<Department> _dept;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IBaseRepository<SysFile> _sysFileRepo;


        public DisciplineService(ApplicationDbContext dbContext, UserManager<AspNetUser> userManager,
            IBaseRepository<Discipline> disciplineRepo, IMapper mapper, IBaseRepository<SysFile> sysFileRepo) : base(disciplineRepo, mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException("context");
            _discipline = dbContext.Set<Discipline>();
            _dept = dbContext.Set<Department>();
            _userManager = userManager;
            _disciplineRepo = disciplineRepo;
            _sysFileRepo = sysFileRepo;
            _mapper = mapper;
        }

        public async Task<ResponseResult> Search(RewardFilterVModel model)
        {
            var result = new ResponseResult();

            // Khởi tạo danh sách các UserId và phòng ban nếu có
            var records = await _discipline.Where(x => x.IsActive == true).ToListAsync();
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

            var list = new List<DisciplineGetAllVModel>();

            // Duyệt qua các bản ghi và ánh xạ
            foreach (var entity in records)
            {
                var user = usersQuery.FirstOrDefault(x => x.Id == entity.UserId);
                if (user == null) continue;

                var vmodel = _mapper.Map<DisciplineGetAllVModel>(entity);
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
                ? (model.IsDescending ? list.OrderByDescending(r => r.CreatedDate) : list.OrderBy(r => r.CreatedDate)).ToList()
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

        public async Task UpdateIsPenalized(UpdateIsPenalizedVModel model)
        {
            var discipline = await _disciplineRepo.GetById(model.Id);

            if (discipline == null)
            {
                throw new BadRequestException("Không tìm thấy kỷ luật!");
            }

            discipline.IsPenalized = !discipline.IsPenalized;

            var updated = await _disciplineRepo.Update(discipline);
            if (updated.Success == false)
            {
                throw new BadRequestException("Cập nhật không thành công!");
            }
        }

        public async Task<ExportStream> ExportFile(RewardFilterVModel model, ExportFileVModel exportModel)
        {
            model.IsExport = true;
            var result = await Search(model);

            var records = _mapper.Map<IEnumerable<DisciplineExportVModel>>(result.Data?.Records);
            var exportData = ImportExportHelper<DisciplineExportVModel>.ExportFile(exportModel, records);
            return exportData;
        }
        public override async Task Create(DisciplineCreateVModel model)
        {
            var disciplineCreate = _mapper.Map<DisciplineCreateVModel, Discipline>(model);
            disciplineCreate.Date = DateTime.Now;
            var createdResult = await _disciplineRepo.Create(disciplineCreate);
            //await base.Create(model);

            if (!createdResult.Success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorCreate, "Object"));
            }
        }

        public async Task<ResponseResult> GetTotalDisciplines(int years, int month)
        {
            var result = new ResponseResult();

            var previousMonth = month == 1 ? 12 : month - 1;
            var previousYear = month == 1 ? years - 1 : years;

            var firstDayOfMonth = new DateTime(years, month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var firstDayOfPreviousMonth = new DateTime(previousYear, previousMonth, 1);
            var lastDayOfPreviousMonth = firstDayOfPreviousMonth.AddMonths(1).AddDays(-1);

            var disciplines = await _dbContext.Discipline.Where(c => c.Date <= lastDayOfMonth && c.Date >= firstDayOfPreviousMonth).ToListAsync();

            var disciplinesInMonth = disciplines.Count(c =>
                c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth);

            var disciplinesInPreviousMonth = disciplines.Count(c =>
                c.Date <= lastDayOfPreviousMonth && c.Date >= firstDayOfPreviousMonth);

            var disciplinepercent = 0;
            if (disciplinesInPreviousMonth == 0)
            {
                disciplinepercent = 100;
            }
            else
            {
                disciplinepercent = (disciplinesInMonth - disciplinesInPreviousMonth) * 100 / disciplinesInPreviousMonth;
            }


            result.Data = new
            {
                TotalBenefit = disciplinesInMonth,
                BenefitPercent = disciplinepercent,
            };
            return result;
        }

        public Task<ResponseResult> GetTotalDisciplineByEmployeeInMonth(int year, int month)
        {
            var result = new ResponseResult();

            try
            {
                var disciplineStats = new List<object>();

                var firstDayOfMonth = new DateTime(year, month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var disciplines = _dbContext.Reward
                        .Where(r => r.Date >= firstDayOfMonth && r.Date <= lastDayOfMonth)
                        .GroupBy(r => r.UserId)
                        .Select(g => new
                        {
                            UserId = g.Key,
                            RewardCount = g.Count(),
                            FullName = _dbContext.AspNetUsers
                                .Where(u => u.Id == g.Key)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                        })
                        .OrderByDescending(r => r.RewardCount) // Sắp xếp giảm dần
                        .ToList();
                foreach (var discipline in disciplines)
                {
                    disciplineStats.Add(new
                    {
                        FullNames = discipline.FullName,
                        TotalRewards = discipline.RewardCount
                    });
                }

                result.Data = new
                {
                    DataDisciplines = disciplineStats
                };

            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return Task.FromResult(result); // Bọc result trong Task
        }

        public async Task<ResponseResult> GetDisciplineStatInYear(int year)
        {
            var result = new ResponseResult();
            try
            {
                var yearStats = new List<object>();

                for (int month = 1; month <= 12; month++)
                {

                    var firstDayOfMonth = new DateTime(year, month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);



                    var Discipline = await _dbContext.Discipline.Where(c => c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth).ToListAsync();

                    var disciplinesInMonth = Discipline.Count(c =>
                        c.Date <= lastDayOfMonth && c.Date >= firstDayOfMonth);




                    yearStats.Add(new
                    {
                        Month = month,
                        DisciplinesInMonths = disciplinesInMonth,
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
        public async Task<ResponseResult> GetMeDisciplineInfo(DisciplineFilterVModel model, int year)
        {
            var result = new ResponseResult();
            try
            {
                var userId = GlobalVariables.GlobalUserId != null ? GlobalVariables.GlobalUserId : string.Empty;
                var disciplineList = await _dbContext.Discipline.Where(x => x.IsActive && x.UserId == userId && x.Date.Year == year).ToListAsync();
                string? keyword = model.Keyword?.ToLower();
                var salaryAns = disciplineList.Where(x =>
                            (x.IsActive == model.IsActive) &&

                            (string.IsNullOrEmpty(keyword) ||
                                    x.Reason.ToLower().Contains(keyword) ||
                                    x.Date.ToString().ToLower().Contains(keyword) ||
                                    x.Money.ToString().ToLower().Contains(keyword) ||
                                    x.Note.ToLower().Contains(keyword)

                            ));
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
