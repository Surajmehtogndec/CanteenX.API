using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CanteenX.API.Services.Order;
using CanteenX.API.DTOs.Orders;
using System.Security.Claims;

namespace CanteenX.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }


        //create order

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequestDto request,CancellationToken cancellationToken)
        {
            try
            {
                string? userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User identity not found."
                    });
                }

                var result = await _orderService.CreateOrderAsync(userId, request, cancellationToken);
                _logger.LogInformation(
                   "Order {OrderId} created successfully by user {UserId}",
                   result.OrderId,
                   userId);


                return CreatedAtAction(
                  nameof(GetOrderById),
                  new { orderId = result.OrderId },
                  new
                  {
                      success = true,
                      message = result.Message,
                      data = result
                  });

            }


            catch(ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }

            catch(InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the order.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
        }



        // ==========================================
        // 2. GET ORDER BY ID
        // GET: api/order/5
        // ==========================================
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrderById(int orderId,CancellationToken cancellationToken)
        {
            if (orderId <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order ID."
                });

            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _orderService.GetOrderByIdAsync(
                orderId,
                userId,
                cancellationToken);

            if (result == null)
                return NotFound(new
                {
                    success = false,
                    message = "Order not found."
                });

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _orderService.GetMyOrdersAsync(
                userId,
                cancellationToken);

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }


        [HttpPatch("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(
            int orderId,
            CancellationToken cancellationToken)
        {
            if (orderId <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order ID."
                });


            string? userId = User.FindFirstValue(
              ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _orderService.CancelOrderAsync(
               orderId,
               userId,
               cancellationToken);


            if (!result)
                return BadRequest(new
                {
                    success = false,
                    message = "Order could not be cancelled. It may not exist or may no longer be eligible for cancellation."
                });
            _logger.LogInformation(
                "Order {OrderId} cancelled by user {UserId}",
                orderId,
                userId);

            return Ok(new
            {
                success = true,
                message = "Order cancelled successfully."
            });


        }




    }
}
