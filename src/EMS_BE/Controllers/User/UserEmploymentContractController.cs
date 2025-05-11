using Microsoft.AspNetCore.Mvc;
using OA.Core.Constants;
using OA.Core.Services;
using OA.Core.VModels;
using OA.Domain.VModels;

namespace OA.WebApi.Controllers
{
    [Route(CommonConstants.Routes.BaseRouteUser)]
    [ApiController]
    public class UserEmploymentContractController : ControllerBase
    {
        private readonly IEmploymentContractService _EmploymentContractService;
        private readonly ILogger<UserEmploymentContractController> _logger;

        public UserEmploymentContractController(IEmploymentContractService EmploymentContractService, ILogger<UserEmploymentContractController> logger)
        {
            _EmploymentContractService = EmploymentContractService;
            _logger = logger;
        }



        [HttpGet]
        public async Task<IActionResult> SearchUser()
        {
            var response = await _EmploymentContractService.SearchUser();
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> ExportContractPdf()
        {
            var exportStream = await _EmploymentContractService.ExportPdf();      
            if (exportStream == null || exportStream.Stream == null)
            {
                return NotFound("Không thể tạo file PDF.");
            }
            return File(exportStream.Stream, exportStream.ContentType, exportStream.FileName);  
        }
    }
}
