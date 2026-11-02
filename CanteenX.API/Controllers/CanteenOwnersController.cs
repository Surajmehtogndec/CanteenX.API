using Microsoft.AspNetCore.Mvc;
using CanteenX.API.DTOs.CanteenOwner;
using CanteenX.API.Services.CanteenOwnerService.Interface;

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
