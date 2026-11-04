using Microsoft.AspNetCore.Mvc;
using CanteenX.API.Services.HomeService;

namespace CanteenX.API.Controllers
{
    [Route("api/home")]
    public class HomeController : Controller
    {
     private readonly IHomeService _homeService;
        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }
        [HttpGet("active-canteens")]
        public async Task<IActionResult> GetAllActiveCanteens(CancellationToken cancellationToken)
        {
            try
            {
                var canteens = await _homeService.GetAllActiveCanteensAsync(cancellationToken);
                return Ok(canteens);
            }
            catch (OperationCanceledException)
            {
                // User ne request cancel kar di ya timeout ho gaya
                return StatusCode(499, "Request was cancelled by the user.");
            }
            catch (Exception)
            {
                // Database ya query fatne par error log karega
                // _logger.LogError(ex, "Database error occurred while fetching active canteens.");
                return StatusCode(500, "An error occurred while fetching active canteens.");
            }
        }

        [HttpGet("available-items")]
        public async Task<IActionResult> GetAvailableMenuItems(CancellationToken cancellationToken)
        {
            try
            {
                var availableItems = await _homeService.GetAvailableMenuItemsAsync(cancellationToken);
                return Ok(availableItems);
            }
            catch (OperationCanceledException)
            {
                // User ne request cancel kar di ya timeout ho gaya
                return StatusCode(499, "Request was cancelled by the user.");
            }
            catch (Exception)
            {
                // Database ya query fatne par error log karega
                // _logger.LogError(ex, "Database error occurred while fetching popular items.");
                return StatusCode(500, "An error occurred while fetching popular items.");
            }
        }

    }
}
