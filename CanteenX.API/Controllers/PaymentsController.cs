using CanteenX.API.DTOs.Payments;
using CanteenX.API.Services.PaymentsServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanteenX.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class PaymentsController : Controller
    {

        private readonly IPaymentsServices _paymentsServices;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentsServices paymentsServices,ILogger<PaymentsController> logger)
        {
            _paymentsServices = paymentsServices;
            _logger = logger;
        }


        // ==========================================
        // 1. CREATE RAZORPAY ORDER
        // POST: api/payments/create
        // ==========================================
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                string? userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User identity not found."
                    });

                var result =
                    await _paymentsServices.CreatePaymentAsync(
                        userId,
                        request,
                        cancellationToken);

                _logger.LogInformation(
                    "Razorpay order created for OrderId {OrderId}",
                    request.OrderId);

                return Ok(new
                {
                    success = true,
                    message = "Payment order created successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Razorpay payment order.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to create payment order."
                });
            }
        }

        // ==========================================
        // 2. VERIFY RAZORPAY PAYMENT
        // POST: api/payments/verify
        // ==========================================
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment(
            [FromBody] PaymentVerificationDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                string? userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User identity not found."
                    });

                var result =
                    await _paymentsServices.VerifyPaymentAsync(
                        userId,
                        request,
                        cancellationToken);

                if (result.PaymentStatus != "Paid")
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "Payment is not captured yet.",
                        data = result
                    });
                }

                _logger.LogInformation(
                    "Payment verified successfully for OrderId {OrderId}",
                    request.OrderId);

                return Ok(new
                {
                    success = true,
                    message = "Payment verified successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error verifying Razorpay payment.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Payment verification failed."
                });
            }
        }

        // ==========================================
        // 3. GET PAYMENT DETAILS
        // GET: api/payments/order/5
        // ==========================================
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetPaymentByOrderId(
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

            var result =
                await _paymentsServices.GetPaymentByOrderIdAsync(
                    orderId,
                    userId,
                    cancellationToken);

            if (result == null)
                return NotFound(new
                {
                    success = false,
                    message = "Payment record not found."
                });

            return Ok(new
            {
                success = true,
                data = result
            });
        }


    }
}
