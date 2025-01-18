using Microsoft.AspNetCore.Mvc;
using OA.Core.Constants;
using OA.Core.Services;
using OA.Core.VModels;
using OA.Domain.Services;
using OA.WebApi.Controllers;

namespace Employee_Management_System.Controllers.User
{
    [Route(CommonConstants.Routes.BaseRouteUser)]
    [ApiController]
    public class UserDisciplineController : ControllerBase
    {
        private readonly IDisciplineService _service;
        private readonly ILogger _logger;
        protected static string _nameController = "Discipline";
        public UserDisciplineController(IDisciplineService service, ILogger<UserDisciplineController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMeDisciplineInfo([FromQuery] RewardFilterVModel model)
        {
            var response = await _service.GetMeDisciplineInfo(model);
            return Ok(response);
        }
    }
}
