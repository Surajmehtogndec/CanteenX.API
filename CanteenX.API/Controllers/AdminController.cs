using CanteenX.API.DTOs;
using CanteenX.API.DTOs.Admin;
using CanteenX.API.Services.AdminServices;
using CanteenX.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CanteenX.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminServices _adminServices;
        private readonly IAuthService _authService;

        public AdminController(IAdminServices adminServices,IAuthService authService)
        {
            _adminServices = adminServices;
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


        // get: api/Admin/Canteen-Staff

        [HttpGet("canteen-staff")]
        public async Task<IActionResult> GetCanteenStaff()
        {
            var staff = await _adminServices.GetAvailableCanteenStaffAsync();

            return Ok(new
            {
                success = true,
                data = staff
            });
        }

        [HttpPost("canteen")]
        public async Task<IActionResult> AddCanteen([FromForm] CreateCanteenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                     success = false,
                     Message = "Invalid Canteen Data.."
                });
            }

            var result = await _adminServices.AddCanteenAsync(dto);
            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to create canteen." + "The staff may not exist, may not have CanteenStaff role,"

                    + "or may already be assigned to a canteen."

                });
            }


            return Ok(new
            {
                success = true,
                Message = "Canteen Add Successfully."
            });
        }


    }
}
