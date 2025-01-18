using OA.Core.Models;
using OA.Core.VModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OA.Core.Services
{
    public interface IStatsRewardAndDiscipline
    {
        Task<ResponseResult> StatsDisplay(int month, int year);
        Task<ResponseResult> StatsChart(int year);
        Task<ResponseResult> TopUserByMonth(int month, int year);
    }
}
