using AutoMapper;
using ChatGPT.Net.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OA.Core.Constants;
using OA.Core.Models;
using OA.Core.Services;
using OA.Core.VModels;
using OA.Infrastructure.EF.Context;
using OA.Infrastructure.EF.Entities;
using OA.Repository;
using OA.Service.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;


namespace OA.Service
{
    public class MassageService : GlobalVariables, IMassageService
    {
        private readonly ApplicationDbContext _dbContext;
        private DbSet<Message> _message;
        private readonly IMapper _mapper;
        private string _nameService = "Message";
        public MassageService(ApplicationDbContext dbContext, IMapper mapper, IHttpContextAccessor contextAccessor) : base(contextAccessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException("context");
            _message = dbContext.Set<Message>();
            _mapper = mapper;
        }

        public async Task Create(MessageCreateVModel model)
        {
            var entity = _mapper.Map<MessageCreateVModel, Message>(model);
            entity.CreatedAt = DateTime.Now;
            entity.Content = model.Content;
            entity.UserId = GlobalUserId != null ? GlobalUserId : string.Empty;

            _message.Add(entity);
            bool success = await _dbContext.SaveChangesAsync() > 0;
            if (!success)
            {
                throw new BadRequestException(string.Format(MsgConstants.ErrorMessages.ErrorCreate, _nameService));
            }
        }

        public async Task<ResponseResult> GetAll()
        {
            var result = new ResponseResult();
            try
            {
                var messageList = await _message.AsQueryable().ToListAsync();
                result.Data = messageList;
            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return result;
        }

        public async Task<ResponseResult> GetMeMessage()
        {
            var result = new ResponseResult();
            try
            {
                var user = GlobalUserId == null ? string.Empty : GlobalUserId;
                var messageList = await _message.Where(x => x.UserId == user).ToListAsync();
                result.Data = messageList;
            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return result;
        }

        public async Task<ResponseResult> GetAllMessage(int type)
        {
            var result = new ResponseResult();
            try
            {
                var date = DateTime.Now.Date; // Lấy ngày hiện tại (không bao gồm thời gian)
                DateTime startDate, endDate;

                switch (type)
                {
                    case 1:
                        startDate = date.AddDays(-(int)date.DayOfWeek).Date; // Đầu tuần (Chủ nhật)
                        endDate = startDate.AddDays(6).Date; // Cuối tuần (Thứ bảy)
                        break;
                    case 2:
                        startDate = new DateTime(date.Year, date.Month, 1).Date; // Đầu tháng
                        endDate = startDate.AddMonths(1).AddDays(-1).Date; // Cuối tháng
                        break;
                    case 3:
                        startDate = new DateTime(date.Year, 1, 1).Date; // Đầu năm
                        endDate = new DateTime(date.Year, 12, 31).Date; // Cuối năm
                        break;
                    default: // Mặc định là ngày
                        startDate = date.Date; // Ngày hiện tại
                        endDate = date.Date; // Ngày hiện tại
                        break;
                }

                var messageList = await _message.Where(x => x.Type == true && x.CreatedAt.Date >= startDate && x.CreatedAt.Date <= endDate).Select(x => x.Content).ToListAsync();
                result.Data = messageList;
            }
            catch (Exception ex)
            {
                throw new BadRequestException(Utilities.MakeExceptionMessage(ex));
            }
            return result;
        }
    }
}
