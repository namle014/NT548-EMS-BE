using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OA.Core.Constants;
using OA.Core.Services;
using OA.Core.VModels;
using OA.Domain.Services;
using OA.Infrastructure.EF.Entities;
namespace OA.WebApi.Controllers
{
    [Route(CommonConstants.Routes.BaseRouteUser)]
    [ApiController]

    public class UserWorkingRulesController : ControllerBase
    {
        private readonly IWorkingRulesService _workingrulesService;
        private readonly ILogger<UserWorkingRulesController> _logger;

        public UserWorkingRulesController(IWorkingRulesService workingrulesService, ILogger<UserWorkingRulesController> logger)
        {
            _workingrulesService = workingrulesService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] WorkingRulesFilterVModel model)
        {
            var response = await _workingrulesService.Search(model);
            return Ok(response);
        }

        

    }
}