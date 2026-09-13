using CanteenX.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CanteenX.API.DTOs;
namespace CanteenX.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }



        [HttpPost("CanteenStaffRegister")]
        public async Task<IActionResult> RegisterCanteenStaff([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAdminAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
