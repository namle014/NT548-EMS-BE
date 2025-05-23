using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OA.Core.Constants;
using OA.Core.Services;
using OA.Domain.Services;
using OA.Domain.VModels;
using OA.Service;

namespace OA.WebAPI.Controllers
{
    [Route(CommonConstants.Routes.BaseRoute)]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private readonly IAspNetUserService _userService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IAspNetUserService userService)
        {
            _authService = authService;
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] CredentialsVModel model)
        {
            ObjectResult result;
            var claimsIdentity = await _authService.Login(model);
            result = new ObjectResult(claimsIdentity);
            return result;
        }

        // /api/Auth/GetRandomNumber
        // [HttpGet]
        // [AllowAnonymous]
        // public async Task<IActionResult> GetRandomNumber()
        // {
        //     // Simulate async work
        //     await Task.Yield();
        //
        //     var random = new Random();
        //     int number = random.Next(1, 101);
        //
        //     var response = new { RandomNumber = number };
        //
        //     // Return the result
        //     return Ok(response);
        // }
        
    [HttpGet]
        public async Task<IActionResult> ExportContractPdf()
        {
            var exportStream = await _authService.ExportPdf();
            if (exportStream == null || exportStream.Stream == null)
            {
                return NotFound("Không thể tạo file PDF.");
            }
            return File(exportStream.Stream, exportStream.ContentType, exportStream.FileName);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            ObjectResult result;
            var response = await _authService.Me();
            result = new ObjectResult(response);
            if (!response.Success)
            {
                _logger.LogWarning(CommonConstants.LoggingEvents.GetItem, MsgConstants.ErrorMessages.ErrorGetById, StringConstants.ControllerName.Auth);
            }
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestResetPassword model)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }
            else
            {
                await _userService.RequestPasswordReset(model);
            }
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }
            else
            {
                await _userService.ResetPassword(model);
            }
            return NoContent();
        }
    }
}