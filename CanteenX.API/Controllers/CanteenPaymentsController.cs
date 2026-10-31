using CanteenX.API.DTOs.Payments;
using CanteenX.API.Services.PaymentsServices.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CanteenX.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class CanteenPaymentsController : Controller
    {
        private readonly IPaymentsServices _paymentsServices;
        public CanteenPaymentsController(IPaymentsServices paymentsServices)
        {
            _paymentsServices = paymentsServices;
        }

        [HttpPost("link-bank")]

        public async Task<IActionResult> LinkBankAccount([FromBody] CanteenAccountDetailsDto dto,CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    Message = "Invalid Canteen Data.."
                });
            }

            var result = await _paymentsServices.AddCanteenAccountAsync(dto,cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new { message = result.Message });
            }
            return StatusCode(500, result.Message);
        }
        
    }
}
