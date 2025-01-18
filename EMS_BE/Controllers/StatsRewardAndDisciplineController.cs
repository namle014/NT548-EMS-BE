using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OA.Core.Constants;
using OA.Core.Services;
using OA.WebApi.Controllers;

namespace EMS_BE.Controllers
{
    [Route(CommonConstants.Routes.BaseRouteAdmin)]
    [ApiController]
    public class StatsRewardAndDisciplineController : ControllerBase
    {
        private readonly IStatsRewardAndDiscipline _service;

        private readonly ILogger _logger;

        public StatsRewardAndDisciplineController(IStatsRewardAndDiscipline service, ILogger<StatsRewardAndDisciplineController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> StatsDisplay([FromQuery] int month, [FromQuery] int year)
        {
            var response = await _service.StatsDisplay(month, year);

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> StatsChart([FromQuery] int year)
        {
            var response = await _service.StatsChart(year);

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> TopUserByMonth([FromQuery] int month, [FromQuery] int year)
        {
            var response = await _service.TopUserByMonth(month, year);

            return Ok(response);
        }
    }
}
