using CanteenX.API.Configurations;
using CanteenX.API.Data;
using CanteenX.API.DTOs;
using CanteenX.API.DTOs.Payments;
using CanteenX.API.Model;
using CanteenX.API.Services.PaymentsServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CanteenX.API.Services.PaymentsServices
{
    public class PaymentsServices : IPaymentsServices
    {
        private readonly CanteenxdbContext _context;
        private readonly RazorpaySettings _razorpaySettings;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        public PaymentsServices(CanteenxdbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory, IOptions<RazorpaySettings> razorpayOptions)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _razorpaySettings = razorpayOptions.Value;
        }



        // create Razorpay order
        public async Task<PaymentResponseDto> CreatePaymentAsync(string userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default) 
        {
          if(string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Invalid User.");

          // fetch student order
          var order = await _context.Orders
                .FirstOrDefaultAsync(
                  x => x.OrderId == request.OrderId && 
                  x.UserId == userId, cancellationToken);

            if (order == null)
                throw new InvalidOperationException("Order not found.");


            if (order.PaymentStatus == "Paid")
                throw new InvalidOperationException(
                    "This order has already been paid.");


            if (order.OrderStatus == "Cancelled")
                throw new InvalidOperationException(
                    "Cancelled orders cannot be paid.");


            // Convert order amount into decimal
            if (!decimal.TryParse(
                order.TotalAmount,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal amountInRupees) ||
                amountInRupees <= 0)
            {
                throw new InvalidOperationException(
                    "Invalid order amount.");
            }
            // Razorpay expects amount in paise
            decimal paiseAmount = amountInRupees * 100;

            if (paiseAmount != decimal.Truncate(paiseAmount) ||
                paiseAmount > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Invalid or unsupported payment amount.");
            }


            int amountInPaise = decimal.ToInt32(paiseAmount);

            // Create Razorpay client
            RazorpayClient client = new RazorpayClient(
                _razorpaySettings.KeyId,
                _razorpaySettings.KeySecret);
            // Prepare Razorpay order data
            Dictionary<string, object> options =
                new Dictionary<string, object>
                {
                    { "amount", amountInPaise },
                    { "currency", _razorpaySettings.Currency },
                    { "receipt", $"CX-{order.OrderId}-{Guid.NewGuid():N}"[..30] },
                    { "partial_payment", false }
                };

            // Create order on Razorpay
            var razorpayOrder = client.Order.Create(options);

            string razorpayOrderId =
               razorpayOrder["id"].ToString()!;

            // Save payment attempt in database
            var payment = new Payments
            {
                OrderId = order.OrderId,
                Provider = "Razorpay",
                RazorPayOrderId = razorpayOrderId,
                RazorpayPaymentId = null,
                Amount = amountInRupees,
                Currency = _razorpaySettings.Currency,
                PaymentStatus = "Created",
                PaymentMethod = null!,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync(cancellationToken);

            return new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                RazorPayOrderId = payment.RazorPayOrderId,
                Amount = amountInPaise,
                Currency = payment.Currency,
                PaymentStatus = payment.PaymentStatus,
                RazorpayKeyId = _razorpaySettings.KeyId
            };


        }


        // 2. VERIFY RAZORPAY PAYMENT

        public async Task<PaymentResponseDto> VerifyPaymentAsync(string userId, PaymentVerificationDto request, CancellationToken cancellationToken = default)
        {

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Invalid user.");

            if (request.OrderId <= 0 ||
                    string.IsNullOrWhiteSpace(request.RazorPayOrderId) ||
                    string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                    string.IsNullOrWhiteSpace(request.RazorpaySignature))
            {
                throw new ArgumentException("Payment verification details are required.");
            }



            // Find payment using server-side saved Razorpay order ID
            var payment = await _context.Payments
                .Include(x => x.Order)
                .ThenInclude(o => o.CanteenOrders)
                .FirstOrDefaultAsync(
                    x => x.OrderId == request.OrderId &&
                         x.RazorPayOrderId == request.RazorPayOrderId &&
                         x.Order.UserId == userId,
                    cancellationToken);

            if (payment == null)
                throw new InvalidOperationException(
                    "Payment record not found.");


            // Prevent duplicate verification
            if (payment.PaymentStatus == "Paid")
            {
                if (payment.RazorpayPaymentId ==
                    request.RazorpayPaymentId)
                {
                    return MapPaymentResponse(payment);
                }

                throw new InvalidOperationException(
                    "This order has already been paid.");
            }

            // Generate expected HMAC SHA256 signature
            string payload =
                payment.RazorPayOrderId + "|" +
                request.RazorpayPaymentId;

            using var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    _razorpaySettings.KeySecret));

            byte[] hash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(payload));

            string expectedSignature =
                Convert.ToHexString(hash).ToLowerInvariant();




            // Compare signatures safely
            bool isSignatureValid =
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature),
                    Encoding.UTF8.GetBytes(
                        request.RazorpaySignature.ToLowerInvariant()));



            if (!isSignatureValid)
                throw new InvalidOperationException(
                    "Invalid Razorpay payment signature.");


            // Fetch payment directly from Razorpay
            RazorpayClient client = new RazorpayClient(
                _razorpaySettings.KeyId,
                _razorpaySettings.KeySecret);

            var razorpayPayment = client.Payment.Fetch(
                request.RazorpayPaymentId);

            string razorpayOrderId =
                razorpayPayment.Attributes.ContainsKey("order_id")
                ? razorpayPayment["order_id"]?.ToString() ?? string.Empty
                : string.Empty;

            string razorpayStatus = razorpayPayment.Attributes.ContainsKey("status")
                ? razorpayPayment["status"]?.ToString() ?? string.Empty
                : string.Empty;

            string rawAmount = razorpayPayment.Attributes.ContainsKey("amount")
                ? razorpayPayment["amount"]?.ToString() ?? "0"
                : "0";

            if (!int.TryParse(rawAmount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int razorpayAmount))
                throw new InvalidOperationException("Invalid payment amount received from Razorpay.");

            string razorpayCurrency = razorpayPayment.Attributes.ContainsKey("currency")
                ? razorpayPayment["currency"]?.ToString() ?? string.Empty
                : string.Empty;

            string paymentMethod = razorpayPayment.Attributes.ContainsKey("method")
                ? razorpayPayment["method"]?.ToString() ?? "Unknown"
                : "Unknown";

            // Verify payment belongs to the saved Razorpay order
            if (razorpayOrderId != payment.RazorPayOrderId)
                throw new InvalidOperationException(
                    "Payment does not belong to this Razorpay order.");

            int expectedPaise = decimal.ToInt32(payment.Amount * 100);

            // Verify amount and currency
            if (razorpayAmount != expectedPaise || razorpayCurrency != payment.Currency)
            {
                throw new InvalidOperationException("Payment amount or currency mismatch.");
            }

            // Update payment record
            payment.RazorpayPaymentId = request.RazorpayPaymentId;
            payment.PaymentMethod = paymentMethod;
            payment.PaymentStatus = razorpayStatus;
            payment.UpdatedAt = DateTime.UtcNow;

            // Mark order paid only when Razorpay confirms capture
            if (razorpayStatus == "captured")
            {
                payment.PaymentStatus = "Paid";

                payment.Order.PaymentStatus = "Paid";
                payment.Order.OrderStatus = "Confirmed";
                payment.Order.UpdatedAt = DateTime.UtcNow;

                foreach (var canteenOrder in payment.Order.CanteenOrders)
                {
                    canteenOrder.OrderStatus = "Confirmed";
                    canteenOrder.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);
                return MapPaymentResponse(payment);
            }
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Payment not captured. Current status: {razorpayStatus}");



        }


        // ==========================================
        // 3. GET PAYMENT BY ORDER ID
        // ==========================================
        public async Task<PaymentResponseDto?> GetPaymentByOrderIdAsync(
            int orderId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(x => x.Order)
                .Where(x =>
                    x.OrderId == orderId &&
                    x.Order.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (payment == null)
                return null;

            return MapPaymentResponse(payment);
        }




        // ==========================================
        // 4. MAP PAYMENT RESPONSE
        // ==========================================
        private PaymentResponseDto MapPaymentResponse(
            Payments payment)
        {
            return new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                RazorPayOrderId = payment.RazorPayOrderId ?? string.Empty,
                Amount = decimal.ToInt32(payment.Amount * 100),
                Currency = payment.Currency,
                PaymentStatus = payment.PaymentStatus,
                RazorpayKeyId = _razorpaySettings.KeyId
            };
        }


    }
}
