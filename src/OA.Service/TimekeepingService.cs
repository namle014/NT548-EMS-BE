using AngleSharp.Dom;
using Aspose.Pdf;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OA.Core.Constants;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.Services;
using OA.Core.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Repository;
using OA.Service.Helpers;

namespace OA.Service
{
    public class TimekeepingService : GlobalVariables, ITimekeepingService
    {
        private readonly IBaseRepository<SysConfiguration> _sysConfigRepo;
        private readonly ApplicationDbContext _dbContext;
        private DbSet<Timekeeping> _timekeepings;
        private DbSet<TimeOff> _timeOff;
        private DbSet<Department> _dept;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IBaseRepository<SysFile> _sysFileRepo;
        private readonly IMapper _mapper;
        string _nameService = "Timekeeping";
        private DbSet<Department> _departments;

        public TimekeepingService(ApplicationDbContext dbContext, UserManager<AspNetUser> userManager, IBaseRepository<SysConfiguration> sysConfigRepo,
            IMapper mapper, IHttpContextAccessor contextAccessor, IBaseRepository<SysFile> sysFileRepo) : base(contextAccessor)
        {
            _sysConfigRepo = sysConfigRepo;
            _dbContext = dbContext ?? throw new ArgumentNullException("context");
            _timekeepings = dbContext.Set<Timekeeping>();
            _departments = dbContext.Set<Department>();
            _timeOff = dbContext.Set<TimeOff>();
            _dept = dbContext.Set<Department>();
            _userManager = userManager;
            _mapper = mapper;
            _sysFileRepo = sysFileRepo;
        }

        public async Task<ResponseResult> GetAllDepartments()
        {
            var result = new ResponseResult();
            var data = _departments.AsQueryable();
            result.Data = await data.ToListAsync();
            return result;
        }

        public async Task<ResponseResult> Search(FilterTimekeepingVModel model)
        {
            var result = new ResponseResult();

            var timekeepings = _timekeepings.AsQueryable();

            var query = from timekeeping in _timekeepings
                        join user in _userManager.Users on timekeeping.UserId equals user.Id
                        join dept in _departments on user.DepartmentId equals dept.Id into deptGroup
                        from dept in deptGroup.DefaultIfEmpty()
                        where
                            (model.StartDate == null ||
                                (timekeeping.Date.Date > model.StartDate.GetValueOrDefault().Date ||
                                 (timekeeping.Date.Date == model.StartDate.GetValueOrDefault().Date && timekeeping.CheckInTime >= model.StartDate.GetValueOrDefault().TimeOfDay))) &&
                            (model.EndDate == null ||
                                (timekeeping.Date.Date < model.EndDate.GetValueOrDefault().Date ||
                                 (timekeeping.Date.Date == model.EndDate.GetValueOrDefault().Date && timekeeping.CheckOutTime <= model.EndDate.GetValueOrDefault().TimeOfDay))) &&
                            (model.FullName == null || user.FullName.ToLower().Contains(model.FullName.ToLower()))
                        select new
                        {
                            timekeeping,
                            user.FullName,
                            user.EmployeeId,
                            user.AvatarFileId,
                            DepartmentName = dept.Name ?? string.Empty
                        };

            var timekeepingList = await query.ToListAsync();

            if (!string.IsNullOrEmpty(model.SortBy))
            {
                switch (model.SortBy.ToLower())
                {
                    case "fullname":
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.FullName).ToList()
                            : timekeepingList.OrderBy(t => t.FullName).ToList();
                        break;
                    case "date":
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.timekeeping.Date).ToList()
                            : timekeepingList.OrderBy(t => t.timekeeping.Date).ToList();
                        break;
                    case "department":
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.DepartmentName).ToList()
                            : timekeepingList.OrderBy(t => t.DepartmentName).ToList();
                        break;
                    case "checkintime":
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.timekeeping.CheckInTime).ToList()
                            : timekeepingList.OrderBy(t => t.timekeeping.CheckInTime).ToList();
                        break;
                    case "checkouttime":
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.timekeeping.CheckOutTime).ToList()
                            : timekeepingList.OrderBy(t => t.timekeeping.CheckOutTime).ToList();
                        break;
                    default:
                        // Nếu SortBy không hợp lệ, mặc định sắp xếp theo Date
                        timekeepingList = model.IsDescending
                            ? timekeepingList.OrderByDescending(t => t.timekeeping.Date).ToList()
                            : timekeepingList.OrderBy(t => t.timekeeping.Date).ToList();
                        break;
                }
            }
            else
            {
                // Nếu không có SortBy, mặc định sắp xếp theo Date
                timekeepingList = model.IsDescending
                    ? timekeepingList.OrderByDescending(t => t.timekeeping.Date).ToList()
                    : timekeepingList.OrderBy(t => t.timekeeping.Date).ToList();
            }

            var timekeepingListMapped = new List<TimekeepingGetAllVModel>();

            var skip = (model.PageNumber - 1) * model.PageSize;
            var records = timekeepingList.Skip(skip).Take(model.PageSize).ToList();

            foreach (var record in records)
            {
                var entityMapped = _mapper.Map<Timekeeping, TimekeepingGetAllVModel>(record.timekeeping);
                entityMapped.FullName = record.FullName;

                entityMapped.Department = record.DepartmentName;

                entityMapped.EmployeeId = record.EmployeeId;
                entityMapped.AvatarPath = record.AvatarFileId != null ? "https://localhost:44381/" + (await _sysFileRepo.GetById((int)record.AvatarFileId))?.Path : null;

                timekeepingListMapped.Add(entityMapped);
            }

            result.Data = new Pagination()
            {
                Records = timekeepingListMapped,
                TotalRecords = timekeepingList.Count()
            };

            return result;
        }



        public async Task<ResponseResult> GetById(int id)
        {
            var result = new ResponseResult();

            var entity = await _timekeepings.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(MsgConstants.WarningMessages.NotFoundData);
            }

            var user = await _userManager.FindByIdAsync(entity.UserId);
            if (user == null)
            {
                throw new NotFoundException(string.Format(MsgConstants.WarningMessages.NotFound, "User"));
            }

            var entityMapped = _mapper.Map<Timekeeping, TimekeepingGetByIdVModel>(entity);
            entityMapped.FullName = user.FullName;

            result.Data = entityMapped;

            return result;
        }

        public async Task Create(TimekeepingCreateVModel model)
        {
            var entity = _mapper.Map<TimekeepingCreateVModel, Timekeeping>(model);

            entity.IsActive = CommonConstants.Status.Active;

            _timekeepings.Add(entity);

            bool success = await _dbContext.SaveChangesAsync() > 0;
            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorCreate, _nameService));
            }
        }

        public async Task Update(TimekeepingUpdateVModel model)
        {
            var entity = await _timekeepings.FindAsync(model.Id);
            if (entity == null)
            {
                throw new NotFoundException(MsgConstants.WarningMessages.NotFoundData);
            }

            _mapper.Map(model, entity);

            bool success = await _dbContext.SaveChangesAsync() > 0;

            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorUpdate, _nameService));
            }
        }

        public async Task ChangeStatus(int id)
        {
            var entity = await _timekeepings.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(MsgConstants.WarningMessages.NotFoundData);
            }

            entity.IsActive = !entity.IsActive;

            bool success = await _dbContext.SaveChangesAsync() > 0;

            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorUpdate, _nameService));
            }
        }

        public async Task Remove(int id)
        {
            var entity = await _timekeepings.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(MsgConstants.WarningMessages.NotFoundData);
            }

            _timekeepings.Remove(entity);

            bool success = await _dbContext.SaveChangesAsync() > 0;
            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorRemove, _nameService));
            }
        }

        public async Task<ResponseResult> CreateUser(TimekeepingCreateUserVModel model)
        {
            if (string.IsNullOrEmpty(GlobalUserId))
            {
                throw new ForbiddenException("Vui lòng đăng nhập!");
            }

            var morning_time = await _sysConfigRepo.Where(x => x.Key == "MORNING_TIME" && x.Type == "workship");
            var lunch_break = await _sysConfigRepo.Where(x => x.Key == "LUNCH_BREAK" && x.Type == "workship");
            var afternoon_time = await _sysConfigRepo.Where(x => x.Key == "AFTERNOON_TIME" && x.Type == "workship");
            var quitting_time = await _sysConfigRepo.Where(x => x.Key == "QUITTING_TIME" && x.Type == "workship");
            var ipAddress = (await _sysConfigRepo.Where(x => x.Key == "IP_ADDRESS" && x.Type == "ip")).FirstOrDefault();
            var listIP = new List<string>();

            if (ipAddress != null && !string.IsNullOrEmpty(ipAddress.Value))
            {
                listIP = ipAddress.Value.Split(',')
                                .Select(ip => ip.Trim().Replace("[", "").Replace("]", ""))  // Loại bỏ dấu ngoặc vuông
                                .ToList();
            }

            if (morning_time.Count() == 0)
            {
                throw new BadRequestException("Không có thời gian chấm công sáng trong hệ thống!");
            }

            if (afternoon_time.Count() == 0)
            {
                throw new BadRequestException("Không có thời gian chấm công chiều trong hệ thống!");
            }

            bool status = false;

            if (listIP.Count() != 0)
            {
                if (listIP.Contains(model.IPAddress))
                {
                    status = true;
                }
            }

            var entity = new Timekeeping
            {
                UserId = GlobalUserId,
                Date = DateTime.Now,
                CheckInTime = DateTime.Now.TimeOfDay,
                CheckInIP = model.IPAddress,
                CheckOutTime = null,
                IsActive = true,
                Status = status,
            };

            _timekeepings.Add(entity);

            bool success = await _dbContext.SaveChangesAsync() > 0;
            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorCreate, _nameService));
            }

            var result = new ResponseResult();

            if (status && success)
            {
                result.Data = new
                {
                    Id = entity.Id
                };
            }
            else
            {
                result.Data = new
                {
                    Id = 0
                };
            }

            return result;
        }

        public async Task CheckOut(CheckOutVModel model)
        {
            if (GlobalUserId == "" || GlobalUserId == null)
            {
                throw new ForbiddenException("Vui lòng đăng nhập!");
            }

            var attendance = await _timekeepings.Where(x => x.Id == model.Id).FirstOrDefaultAsync();

            if (attendance == null)
            {
                throw new BadRequestException("Không tìm thấy chấm công!");
            }

            attendance.CheckOutTime = DateTime.Now.TimeOfDay;

            if (attendance.CheckOutTime.HasValue)
            {
                attendance.TotalHours = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalHours;
            }
            else
            {
                attendance.TotalHours = 0;
            }

            _timekeepings.Update(attendance);

            bool success = await _dbContext.SaveChangesAsync() > 0;

            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorUpdate, _nameService));
            }
        }

        public async Task<ResponseResult> SearchForUser(FilterTimekeepingForUserVModel model)
        {
            var result = new ResponseResult();

            if (string.IsNullOrEmpty(GlobalUserId))
            {
                throw new ForbiddenException("Vui lòng đăng nhập!");
            }

            var morning_time = (await _sysConfigRepo.Where(x => x.Key == "MORNING_TIME" && x.Type == "workship")).FirstOrDefault();
            var lunch_break = (await _sysConfigRepo.Where(x => x.Key == "LUNCH_BREAK" && x.Type == "workship")).FirstOrDefault();
            var afternoon_time = (await _sysConfigRepo.Where(x => x.Key == "AFTERNOON_TIME" && x.Type == "workship")).FirstOrDefault();
            var quitting_time = (await _sysConfigRepo.Where(x => x.Key == "QUITTING_TIME" && x.Type == "workship")).FirstOrDefault();

            if (morning_time == null)
            {
                throw new BadRequestException("Không có thời gian chấm công sáng trong hệ thống!");
            }

            if (afternoon_time == null)
            {
                throw new BadRequestException("Không có thời gian chấm công chiều trong hệ thống!");
            }

            if (lunch_break == null)
            {
                throw new BadRequestException("Không có thời gian nghỉ trưa trong hệ thống!");
            }

            if (quitting_time == null)
            {
                throw new BadRequestException("Không có thời gian ra về trong hệ thống!");
            }

            if (!TimeSpan.TryParse(morning_time.Value, out var morningTimeSpan))
            {
                throw new BadRequestException("Không thể chuyển đổi thời gian sáng trong hệ thống!");
            }

            // Chuyển các thời gian khác như lunch_break, afternoon_time, quitting_time thành TimeSpan nếu cần
            TimeSpan lunchBreakTimeSpan;
            if (!TimeSpan.TryParse(lunch_break.Value, out lunchBreakTimeSpan))
            {
                throw new BadRequestException("Không thể chuyển đổi thời gian nghỉ trưa trong hệ thống!");
            }

            TimeSpan afternoonTimeSpan;
            if (!TimeSpan.TryParse(afternoon_time.Value, out afternoonTimeSpan))
            {
                throw new BadRequestException("Không thể chuyển đổi thời gian chiều trong hệ thống!");
            }

            TimeSpan quittingTimeSpan;
            if (!TimeSpan.TryParse(quitting_time.Value, out quittingTimeSpan))
            {
                throw new BadRequestException("Không thể chuyển đổi thời gian ra về trong hệ thống!");
            }

            var records = await _timekeepings
                        .Where(x => x.UserId == GlobalUserId
                                    && (!model.StartDate.HasValue || x.Date >= model.StartDate.Value)
                                    && (!model.EndDate.HasValue || x.Date <= model.EndDate.Value)
                                    && ((model.IsInvalid == true && x.Status == false)
                                    || (model.IsLate == true && (
                                            (x.CheckInTime < new TimeSpan(13, 0, 0) && x.CheckInTime > morningTimeSpan)
                                            || (x.CheckInTime >= new TimeSpan(13, 0, 0) && x.CheckInTime > afternoonTimeSpan)
                                        ))
                                        || (model.IsOnTime == true && (
                                            (x.CheckInTime <= morningTimeSpan && x.CheckInTime <= new TimeSpan(13, 0, 0) && (x.CheckOutTime == null || x.CheckOutTime >= lunchBreakTimeSpan))
                                            || (x.CheckInTime <= afternoonTimeSpan && (x.CheckOutTime == null || x.CheckOutTime >= quittingTimeSpan))
                                        ))
                                        || (model.IsEarly == true && (
                                            (x.CheckOutTime != null &&
                                            ((x.CheckOutTime < lunchBreakTimeSpan && x.CheckOutTime < new TimeSpan(13, 0, 0)) ||
                                                (x.CheckOutTime >= new TimeSpan(13, 0, 0) && x.CheckOutTime < quittingTimeSpan)))
                        ))
                        ))
                        .OrderByDescending(x => x.Date) // Sắp xếp theo Date giảm dần
                        .ThenByDescending(x => x.CheckInTime) // Sau đó sắp xếp theo CheckInTime giảm dần
                        .AsQueryable()
                        .ToListAsync();

            var totalRecords = records.Count();

            var notifications = records
                                    .Skip((model.PageNumber - 1) * model.PageSize)
                                    .Take(model.PageSize)
                                    .ToList();

            result.Success = true;
            result.Data = new Pagination();

            result.Data.TotalRecords = totalRecords;
            result.Data.Records = notifications;

            return result;
        }

        public async Task<ResponseResult> GetByDate(FilterTimekeepingGetByDateVModel model)
        {
            var result = new ResponseResult();

            if (string.IsNullOrEmpty(GlobalUserId))
            {
                throw new ForbiddenException("Vui lòng đăng nhập!");
            }
            var records = await _timekeepings
                        .Where(x => x.UserId == GlobalUserId
                                    && x.Date.Date == model.Date.Date)
                        .ToListAsync();

            result.Data = records;

            return result;
        }

        public async Task<ResponseResult> Stats(FilterTimekeepingGetByDateVModel model)
        {
            var result = new ResponseResult();

            if (string.IsNullOrEmpty(GlobalUserId))
            {
                throw new ForbiddenException("Vui lòng đăng nhập!");
            }

            var records = await _timekeepings.Where(x => x.UserId == GlobalUserId && x.Date == model.Date.Date).ToListAsync();

            var countInValid = records.Where(x => x.Status == false).Count();

            var timeCheckIn = records.Where(x => x.Status == true).OrderByDescending(x => x.Date).FirstOrDefault()?.CheckInTime;

            var attendanceCurrent = records.OrderBy(x => x.Date).FirstOrDefault();

            result.Data = new
            {
                CountLogin = records.Count(),
                CountInvalid = countInValid,
                TimeCheckIn = timeCheckIn,
                Attendance = attendanceCurrent
            };

            return result;
        }

        public async Task<ResponseResult> StatsDisplay(DateTime date)
        {
            var result = new ResponseResult();

            // Tính toán các ngày cần lấy dữ liệu
            var startDate = date.AddDays(-6).Date;
            var endDate = date.Date;
            var yesterday = date.AddDays(-1).Date;

            // Lấy giờ làm việc quy định từ cấu hình hệ thống
            var morningTime = TimeSpan.Parse((await _sysConfigRepo.Where(x => x.Key == "MORNING_TIME" && x.Type == "workship")).FirstOrDefault()?.Value ?? "08:00");
            var lunchBreak = TimeSpan.Parse((await _sysConfigRepo.Where(x => x.Key == "LUNCH_BREAK" && x.Type == "workship")).FirstOrDefault()?.Value ?? "12:00");
            var afternoonTime = TimeSpan.Parse((await _sysConfigRepo.Where(x => x.Key == "AFTERNOON_TIME" && x.Type == "workship")).FirstOrDefault()?.Value ?? "13:00");
            var quittingTime = TimeSpan.Parse((await _sysConfigRepo.Where(x => x.Key == "QUITTING_TIME" && x.Type == "workship")).FirstOrDefault()?.Value ?? "17:00");

            // Lấy dữ liệu chấm công trong khoảng thời gian
            var records = await _timekeepings
                .Where(x => x.Date.Date >= startDate && x.Date.Date <= endDate && x.Status == true)
                .ToListAsync();

            // Lấy dữ liệu nghỉ phép trong khoảng thời gian
            var timeOffRecords = await _timeOff
                .Where(x => x.IsAccepted == true &&
                            (x.StartDate.Date <= endDate && x.EndDate.Date >= startDate))
                .ToListAsync();

            var allUserCount = await _userManager.Users.Where(x => x.IsActive == true).CountAsync();

            // Phân loại chấm công theo ngày
            var statsByDay = Enumerable.Range(0, 7).Select(offset =>
            {
                var currentDay = endDate.AddDays(-offset);
                var dayRecords = records.Where(x => x.Date.Date == currentDay).ToList();

                // Lấy lần đầu tiên chấm công của mỗi nhân viên trong ngày
                var firstRecords = dayRecords
                    .GroupBy(x => x.UserId)
                    .Select(g => g.OrderBy(r => r.CheckInTime).First())
                    .ToList();

                var countOnTime = firstRecords.Count(x => x.CheckInTime <= morningTime && (x.CheckOutTime == null || x.CheckOutTime >= quittingTime));
                var countLeftEarly = firstRecords.Count(x => x.CheckOutTime != null && x.CheckOutTime < quittingTime);
                var countLate = firstRecords.Count(x => x.CheckInTime > morningTime);
                var countLeave = timeOffRecords.Count(x => x.StartDate.Date <= currentDay && x.EndDate.Date >= currentDay);
                var countAbsent = allUserCount - firstRecords.Count - countLeave;
                var countInvalid = dayRecords.Count(x => x.Status == false);

                return new
                {
                    Date = currentDay,
                    OnTime = countOnTime,
                    LeftEarly = countLeftEarly,
                    Late = countLate,
                    Leave = countLeave,
                    Absent = countAbsent,
                    Invalid = countInvalid
                };
            }).ToList();

            var dataSet = new
            {
                OnTime = statsByDay.Select(stat => stat.OnTime).Reverse().ToList(),
                Early = statsByDay.Select(stat => stat.LeftEarly).Reverse().ToList(),
                Late = statsByDay.Select(stat => stat.Late).Reverse().ToList(),
                Leave = statsByDay.Select(stat => stat.Leave).Reverse().ToList(),
                Absent = statsByDay.Select(stat => stat.Absent).Reverse().ToList(),
                Invalid = statsByDay.Select(stat => stat.Invalid).Reverse().ToList()
            };

            // Tính toán tổng ngày hôm nay và hôm qua
            var todayStats = statsByDay.FirstOrDefault(x => x.Date == endDate);
            var yesterdayStats = statsByDay.FirstOrDefault(x => x.Date == yesterday);

            // Tính phần trăm so với ngày hôm qua
            var percentageChange = new
            {
                OnTime = todayStats != null && yesterdayStats != null ?
              Math.Round((todayStats.OnTime - yesterdayStats.OnTime) * 100.0 / (yesterdayStats.OnTime == 0 ? 1 : yesterdayStats.OnTime), 2) : 0,
                LeftEarly = todayStats != null && yesterdayStats != null ?
                 Math.Round((todayStats.LeftEarly - yesterdayStats.LeftEarly) * 100.0 / (yesterdayStats.LeftEarly == 0 ? 1 : yesterdayStats.LeftEarly), 2) : 0,
                Late = todayStats != null && yesterdayStats != null ?
            Math.Round((todayStats.Late - yesterdayStats.Late) * 100.0 / (yesterdayStats.Late == 0 ? 1 : yesterdayStats.Late), 2) : 0,
                Leave = todayStats != null && yesterdayStats != null ?
             Math.Round((todayStats.Leave - yesterdayStats.Leave) * 100.0 / (yesterdayStats.Leave == 0 ? 1 : yesterdayStats.Leave), 2) : 0,
                Absent = todayStats != null && yesterdayStats != null ?
              Math.Round((todayStats.Absent - yesterdayStats.Absent) * 100.0 / (yesterdayStats.Absent == 0 ? 1 : yesterdayStats.Absent), 2) : 0,
                Invalid = todayStats != null && yesterdayStats != null ?
               Math.Round((todayStats.Invalid - yesterdayStats.Invalid) * 100.0 / (yesterdayStats.Invalid == 0 ? 1 : yesterdayStats.Invalid), 2) : 0
            };


            // Trả kết quả
            result.Data = new
            {
                StatsByDay = statsByDay,
                DataSet = dataSet,
                PercentageChange = percentageChange
            };

            return result;
        }

        public async Task<ResponseResult> GetTodayAttendanceSummary(DateTime date)
        {
            var result = new ResponseResult();

            // Xác định ngày hôm nay và ngày hôm qua
            var today = date.Date;
            var yesterday = date.AddDays(-1).Date;

            // Tổng số nhân viên đang hoạt động
            var allUserCount = await _userManager.Users.Where(x => x.IsActive == true).CountAsync();

            // Lấy dữ liệu chấm công cho ngày hôm nay
            var todayRecords = await _timekeepings
                .Where(x => x.Date.Date == today && x.Status == true)
                .ToListAsync();

            // Lấy dữ liệu chấm công cho ngày hôm qua
            var yesterdayRecords = await _timekeepings
                .Where(x => x.Date.Date == yesterday && x.Status == true)
                .ToListAsync();

            // Số nhân viên chấm công hôm nay
            var checkedInToday = todayRecords
                .GroupBy(x => x.UserId)
                .Select(g => g.First())
                .Count();

            // Số nhân viên vắng mặt hôm nay
            var absentToday = allUserCount - checkedInToday;

            // Số nhân viên chấm công hôm qua
            var checkedInYesterday = yesterdayRecords
                .GroupBy(x => x.UserId)
                .Select(g => g.First())
                .Count();

            // Tính tỷ lệ phần trăm
            var attendanceRateToday = (double)checkedInToday / allUserCount * 100;
            double absentRateToday = 100.0 - attendanceRateToday;

            // Tính thay đổi so với hôm qua
            var attendanceRateChange = checkedInYesterday == 0
                ? 0
                : ((double)(checkedInToday - checkedInYesterday) / checkedInYesterday) * 100;

            // Trả kết quả
            result.Data = new
            {
                TotalUsers = allUserCount,
                CheckedIn = checkedInToday,
                Absent = absentToday,
                AttendanceRate = Math.Round(attendanceRateToday, 2),
                AbsentRate = Math.Round(absentRateToday, 2),
                AttendanceRateChange = Math.Round(attendanceRateChange, 2)
            };

            return result;
        }

        public async Task<ResponseResult> GetAttendanceSummary(DateTime date, string period = "day")
        {
            var result = new ResponseResult();

            // Xác định khoảng thời gian
            DateTime startDate, endDate = date.Date;
            switch (period.ToLower())
            {
                case "week":
                    startDate = date.AddDays(-(int)date.DayOfWeek).Date; // Đầu tuần
                    break;
                case "month":
                    startDate = new DateTime(date.Year, date.Month, 1).Date; // Đầu tháng
                    break;
                case "year":
                    startDate = new DateTime(date.Year, 1, 1).Date; // Đầu năm
                    break;
                default: // Mặc định là ngày
                    startDate = date.Date;
                    break;
            }

            // Lấy danh sách tất cả các phòng ban
            var allDepartments = await _dept.ToListAsync();

            // Tổng số nhân viên đang hoạt động
            var allUsers = await _userManager.Users
                .Where(x => x.IsActive == true)
                .ToListAsync();

            // Lấy dữ liệu chấm công trong khoảng thời gian
            var records = await _timekeepings
                .Where(x => x.Date.Date >= startDate && x.Date.Date <= endDate && x.Status == true)
                .ToListAsync();

            // Nhóm dữ liệu theo phòng ban
            //var departmentAttendance = allDepartments.Select(department =>
            //{
            //    var departmentId = department.Id;
            //    var departmentUsers = allUsers.Where(u => u.DepartmentId == departmentId).ToList();
            //    var departmentUserIds = departmentUsers.Select(u => u.Id).ToHashSet();

            //    // Chỉ tính nhân viên có ít nhất một lần chấm công
            //    var checkedInCount = records
            //        .Where(record => departmentUserIds.Contains(record.UserId))
            //        .GroupBy(record => record.UserId)
            //        .Count(g => g.Any());

            //    var totalUsersInDepartment = departmentUsers.Count;

            //    return new
            //    {
            //        DepartmentId = departmentId,
            //        DepartmentName = department.Name,
            //        TotalUsers = totalUsersInDepartment,
            //        CheckedIn = checkedInCount,
            //        Absent = totalUsersInDepartment - checkedInCount,
            //        AttendanceRate = totalUsersInDepartment > 0
            //            ? Math.Round((double)checkedInCount / totalUsersInDepartment * 100, 2)
            //            : 0,
            //        AbsentRate = totalUsersInDepartment > 0
            //            ? Math.Round((double)(totalUsersInDepartment - checkedInCount) / totalUsersInDepartment * 100, 2)
            //            : 0
            //    };
            //}).ToList();

            var departmentAttendance = allDepartments.Select(department =>
            {
                var departmentId = department.Id;

                // Lấy danh sách user trong phòng ban này
                var departmentUsers = allUsers.Where(u => u.DepartmentId == departmentId).ToList();
                var departmentUserIds = departmentUsers.Select(u => u.Id).ToHashSet();

                // Lấy danh sách các ngày trong khoảng thời gian
                var totalDays = Enumerable.Range(0, (endDate - startDate).Days + 1)
                    .Select(offset => startDate.AddDays(offset).Date).ToList();

                // Tính số nhân viên có ít nhất một lần chấm công trong khoảng thời gian
                var checkedInCount = records
                    .Where(record => departmentUserIds.Contains(record.UserId))
                    .Select(record => new { record.UserId, record.Date.Date })
                    .Distinct()
                    .GroupBy(x => x.UserId)
                    .Count();

                // Tổng số lần check-in có thể xảy ra (số user x số ngày)
                var totalPotentialCheckIns = departmentUsers.Count * totalDays.Count;

                // Tổng số lần vắng mặt
                var absentCount = totalPotentialCheckIns - records
                    .Where(record => departmentUserIds.Contains(record.UserId))
                    .Select(record => new { record.UserId, record.Date.Date })
                    .Distinct()
                    .Count();

                // Tính tỷ lệ
                var attendanceRate = totalPotentialCheckIns > 0
                    ? Math.Round((double)(totalPotentialCheckIns - absentCount) / totalPotentialCheckIns * 100, 2)
                    : 0;
                var absentRate = totalPotentialCheckIns > 0
                    ? Math.Round((double)absentCount / totalPotentialCheckIns * 100, 2)
                    : 0;

                // Trả về dữ liệu
                return new object[]
                {
                    department.Name,                   // Tên phòng ban
                    attendanceRate,                    // Tỷ lệ đi làm
                    absentRate                         // Tỷ lệ vắng mặt
                };
            }).ToList();



            // Trả kết quả
            result.Data = new
            {
                TotalDepartments = allDepartments.Count,
                TotalUsers = allUsers.Count,
                StartDate = startDate,
                EndDate = endDate,
                DepartmentSummary = departmentAttendance
            };

            return result;
        }

        public async Task<ResponseResult> GetHourlyAttendanceStats(DateTime date)
        {
            var result = new ResponseResult();

            // Tính toán khoảng thời gian 7 ngày gần nhất
            var startDate = date.AddDays(-6).Date;
            var endDate = date.Date;

            // Lấy danh sách nhân viên đang hoạt động
            var allUsers = await _userManager.Users
                .Where(x => x.IsActive == true)
                .ToListAsync();

            // Lấy dữ liệu chấm công trong khoảng thời gian
            var records = await _timekeepings
                .Where(x => x.Date.Date >= startDate && x.Date.Date <= endDate && x.Status == true)
                .ToListAsync();

            // Khung giờ cần thống kê
            var timeSlots = new List<int> { 7, 8, 10, 12, 14, 16, 18 };

            // Thống kê số lượng nhân viên theo từng ngày và khung giờ
            var dailyStats = new List<object>();
            int globalMax = 0; // Giá trị lớn nhất toàn bộ
            int globalMin = int.MaxValue; // Giá trị nhỏ nhất toàn bộ

            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                var hourlyStats = timeSlots.Select(hour =>
                {
                    // Lấy số lượng nhân viên chấm công trong giờ này
                    var count = records
                        .Where(r => r.Date.Date == day.Date &&
                                   r.CheckInTime.Hours <= hour &&
                                   (r.CheckOutTime == null || r.CheckOutTime?.Hours >= hour)) // Kiểm tra giờ nằm trong khoảng StartTime và EndTime
                        .Select(r => r.UserId)
                        .Distinct()
                        .Count();

                    // Cập nhật giá trị max và min
                    globalMax = Math.Max(globalMax, count);
                    globalMin = Math.Min(globalMin, count);

                    return new
                    {
                        Hour = hour,
                        CheckedInCount = count
                    };
                }).ToList();

                int cnt = 0;

                // Sửa lại phần AddRange và đảm bảo cấu trúc mảng
                dailyStats.AddRange(hourlyStats.Select(hourStat =>
                    new int[] { (int)day.DayOfWeek, cnt++, hourStat.CheckedInCount }
                ));
            }

            // Trả kết quả
            result.Data = new
            {
                StartDate = startDate,
                EndDate = endDate,
                MaxAttendance = globalMax,
                MinAttendance = globalMin,
                DailyStats = dailyStats
            };

            return result;
        }

        public async Task<ResponseResult> GetSummary(string type)
        {
            var result = new ResponseResult();

            var userId = GlobalVariables.GlobalUserId;

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new BadRequestException("Vui lòng đăng nhập!");
            }

            var date = DateTime.Now.Date; // Lấy ngày hiện tại (không bao gồm thời gian)
            DateTime startDate, endDate;

            switch (type.ToLower())
            {
                case "week":
                    startDate = date.AddDays(-(int)date.DayOfWeek).Date; // Đầu tuần (Chủ nhật)
                    endDate = startDate.AddDays(6).Date; // Cuối tuần (Thứ bảy)
                    break;
                case "month":
                    startDate = new DateTime(date.Year, date.Month, 1).Date; // Đầu tháng
                    endDate = startDate.AddMonths(1).AddDays(-1).Date; // Cuối tháng
                    break;
                case "year":
                    startDate = new DateTime(date.Year, 1, 1).Date; // Đầu năm
                    endDate = new DateTime(date.Year, 12, 31).Date; // Cuối năm
                    break;
                default: // Mặc định là ngày
                    startDate = date.Date; // Ngày hiện tại
                    endDate = date.Date; // Ngày hiện tại
                    break;
            }

            var attendRecords = await _timekeepings.Where(x => x.IsActive == true && x.UserId == userId &&
                                                        (x.Date.Date >= startDate.Date) &&
                                                        (x.Date.Date <= endDate.Date)).ToListAsync();

            var countAttendance = attendRecords.Where(x => x.CheckOutTime != null).Count();

            var countLogin = attendRecords.Count();

            result.Data = new
            {
                CountAttendance = countAttendance,
                CountLogin = countLogin,
            };

            return result;
        }
    }
}
