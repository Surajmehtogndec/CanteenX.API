using Microsoft.AspNetCore.Mvc;
using CanteenX.API.DTOs.CanteenOwner;
using CanteenX.API.Services.CanteenOwnerService.Interface;
using System.Security.Claims;

namespace CanteenX.API.Controllers
{
    [Route("api/CanteenStaff")]
    [ApiController]
    public class CanteenOwnersController : Controller
    {
       private readonly ICanteenOwnerService _canteenOwnerService;
        public CanteenOwnersController(ICanteenOwnerService canteenOwnerService)
        {
            _canteenOwnerService = canteenOwnerService;
        }

        [HttpGet("my-canteen-id")]
        public async Task<IActionResult> GetMyCanteenId()
        {
            // Logged-in user ki ID nikalna
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not logged in." });
            }

            var canteenId = await _canteenOwnerService.GetCanteenIdByUserIdAsync(userId);

            if (canteenId == 0)
            {
                return NotFound(new { message = "No canteen linked to this user." });
            }

            // JSON format mein CanteenId return kar rahe hain
            return Ok(new { canteenId = canteenId });
        }
        [HttpPost("add-menu-item")]
        public async Task<IActionResult> AddMenuItem([FromForm] AddMenuItemDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data provided."
                });
            }
            var result = await _canteenOwnerService.AddMenuItemAsync(dto, cancellationToken);
            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to add menu item. Please check the provided data."
                });
            }
            return Ok(new
            {
                success = true,
                message = "Menu item added successfully."
            });
        }
    }
}
