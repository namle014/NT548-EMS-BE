using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OA.Core.Constants;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.VModels;
using OA.Domain.Services;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Service.Helpers;
using System.Reflection.Metadata.Ecma335;
//using Twilio.TwiML.Voice;

namespace OA.Service
{
    public class DepartmentService : BaseService<Department, DepartmentCreateVModel, DepartmentUpdateVModel, DepartmentGetByIdVModel, DepartmentGetAllVModel, DepartmentExportVModel>, IDepartmentService
    {
        private readonly IBaseRepository<Department> _departmentRepo;
        private readonly IMapper _mapper;
        private DbSet<Department> _dbSet;
        private readonly ApplicationDbContext _dbContext;


        public DepartmentService(ApplicationDbContext dbContext, IBaseRepository<Department> departmentRepo, IMapper mapper) : base(departmentRepo, mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException("context");

            _departmentRepo = departmentRepo;
            _mapper = mapper;
            _dbSet = dbContext.Set<Department>();
        }

        public async Task<ResponseResult> Search(DepartmentFilterVModel model)
        {
            var result = new ResponseResult();

            string? keyword = model.Keyword?.ToLower();

            var records = await _dbContext.Department.OrderBy(x => x.Id).ToListAsync();
                        

            result.Data = new Pagination();

        
            var list = new List<DepartmentGetAllVModel>();
            foreach (var entity in records)
            {
                var vmodel = _mapper.Map<DepartmentGetAllVModel>(entity);
                var departmentId = entity.Id;
                var countEntity = await _dbContext.AspNetUsers.Where(x => x.DepartmentId != null && x.DepartmentId == departmentId && x.IsActive).CountAsync();
                var userNames = await _dbContext.AspNetUsers
                    .Where(x=> x.Id == entity.DepartmentHeadId)
                    .Select(x => x.FullName).FirstOrDefaultAsync();

                var departmentEmployeeId = await _dbContext.AspNetUsers
                    .Where(x => x.Id == entity.DepartmentHeadId)
                    .Select(x => x.EmployeeId).FirstOrDefaultAsync();
                if (countEntity > 0)
                {
                    vmodel.CountDepartment = countEntity;
                }
                vmodel.DepartmentHeadName = userNames;
                vmodel.DepartmentHeadEmployeeId = departmentEmployeeId;
                vmodel.IsActive = entity.IsActive;

               list.Add(vmodel);
            }


            list = list.Where(x =>
                            (x.IsActive == true) &&
                            (model.CreatedDate == null ||
                                    (x.CreatedDate.HasValue &&
                                    x.CreatedDate.Value.Year == model.CreatedDate.Value.Year &&
                                    x.CreatedDate.Value.Month == model.CreatedDate.Value.Month &&
                                    x.CreatedDate.Value.Day == model.CreatedDate.Value.Day)) &&
                            (string.IsNullOrEmpty(keyword) ||
                                    (x.Name != null && x.Name.ToLower().Contains(keyword)) ||
                                    (x.DepartmentHeadName != null && x.DepartmentHeadName.ToLower().Contains(keyword)) ||
                                    (x.DepartmentHeadEmployeeId != null && x.DepartmentHeadEmployeeId.ToLower().Contains(keyword)) ||
                                    (x.CreatedBy != null && x.CreatedBy.ToLower().Contains(keyword))
                                    )).ToList(); 


            if (!model.IsDescending)
            {
                list = string.IsNullOrEmpty(model.SortBy)
                    ? list.OrderBy(r => r.Id).ToList()
                    : list.OrderBy(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null)).ToList();
            }
            else
            {
                list = string.IsNullOrEmpty(model.SortBy)
                    ? list.OrderByDescending(r => r.Id).ToList()
                    : list.OrderByDescending(r => r.GetType().GetProperty(model.SortBy)?.GetValue(r, null)).ToList();
            }

            var pagedRecords = list.Skip((model.PageNumber - 1) * model.PageSize).Take(model.PageSize).ToList();


            result.Data.Records = pagedRecords;
            result.Data.TotalRecords = list.Count;
           

            return result;
        }

        public async Task<ExportStream> ExportFile(DepartmentFilterVModel model, ExportFileVModel exportModel)
        {
            model.IsExport = true;
            var result = await Search(model);

            var records = _mapper.Map<IEnumerable<DepartmentExportVModel>>(result.Data?.Records);
            var exportData = ImportExportHelper<DepartmentExportVModel>.ExportFile(exportModel, records);
            return exportData;
        }

        public async Task<ResponseResult> GetAllDepartments()
        {
            var result = new ResponseResult();

            var records = await _dbContext.Department.ToListAsync();

            var listsDepartment = new List<DepartmentGetAllVModel>();
            foreach (var list in records)
            {
                var model = _mapper.Map<DepartmentGetAllVModel>(list);
                var departmentId = list.Id;
                var countEntity = await _dbContext.AspNetUsers.Where(x => x.DepartmentId != null && x.DepartmentId == departmentId && x.IsActive).CountAsync();
                var userNames = await _dbContext.AspNetUsers
                    .Where(x => x.DepartmentId == departmentId && x.IsActive && x.Id == list.DepartmentHeadId)
                    .Select(x => x.FullName).FirstOrDefaultAsync();
                if (countEntity > 0)
                {
                    model.CountDepartment = countEntity;
                }
                model.DepartmentHeadName = userNames;
                listsDepartment.Add(model);
            }

            result.Data = new Pagination
            {
                Records = listsDepartment,
                TotalRecords = listsDepartment.Count
            };
            return result;
        }

        public async Task ChangeStatusMany(DepartmentChangeStatusManyVModel model)
        {
            if (model?.Ids == null || !model.Ids.Any())
            {
                throw new NotFoundException(MsgConstants.WarningMessages.NotFoundData);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Lấy danh sách các thực thể cần cập nhật
                var entitiesToUpdate = await _dbSet.Where(x => model.Ids.Contains(x.Id)).ToListAsync();

                // Kiểm tra xem có thiếu ID nào không
                var missingIds = model.Ids.Except(entitiesToUpdate.Select(x => x.Id)).ToList();
                if (missingIds.Any())
                {
                    throw new NotFoundException(string.Format(MsgConstants.WarningMessages.NotFound, string.Join(", ", missingIds)));
                }

                // Cập nhật giá trị IsActive
                foreach (var entity in entitiesToUpdate)
                {
                    entity.IsActive = !entity.IsActive; // Đảo ngược trạng thái IsActive
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                await _dbContext.SaveChangesAsync();

                // Commit giao dịch
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Rollback nếu xảy ra lỗi
                await transaction.RollbackAsync();
                throw new BadRequestException($"Transaction failed: {ex.Message}");
            }
        }

        public override async Task Create(DepartmentCreateVModel model)
        {
            var Create = _mapper.Map<DepartmentCreateVModel, Department>(model);
            var createdResult = await _departmentRepo.Create(Create);
            if (!createdResult.Success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorCreate, "Object"));
            }
        }

        public override async Task Update(DepartmentUpdateVModel model)
        {
            var Update = _mapper.Map<DepartmentUpdateVModel, Department>(model);
            var UpdateResult = await _departmentRepo.Update(Update);
            if (!UpdateResult.Success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorUpdate, "Object"));
            }
        }

    }
}
