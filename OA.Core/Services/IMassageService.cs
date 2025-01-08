using OA.Core.Models;
using OA.Core.VModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OA.Core.Services
{
    public interface IMassageService
    {
        Task Create(MessageCreateVModel model);
        Task<ResponseResult> GetAll();
        Task<ResponseResult> GetMeMessage();
    }
}
