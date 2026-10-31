using CanteenX.API.Data;
using CanteenX.API.DTOs.Payments;
using CanteenX.API.Model;
using CanteenX.API.Services.PaymentsServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CanteenX.API.Services.PaymentsServices
{
    public class PaymentsServices : IPaymentsServices
    {
        private readonly CanteenxdbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        public PaymentsServices(CanteenxdbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }
       
        // yeha hum canteen payment account details add karne ka login implement kr rahe hai 
        public async Task<(bool IsSuccess, string Message)> AddCanteenAccountAsync(CanteenAccountDetailsDto dto, CancellationToken cancellationToken = default)
        {

            var canteen = await _context.Canteens.FirstOrDefaultAsync(c => c.UserId == dto.UserId, cancellationToken);

            if (canteen == null)
            {
                return (false, "Is user ke saath koi canteen link nahi hai.");
            }
            int realCanteenId = canteen.CanteenId;
            string keyId = _configuration["Razorpay:KeyId"] ?? "";
            string keySecret = _configuration["Razorpay:KeySecret"] ?? "";

            var client = _httpClientFactory.CreateClient();
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            try
            {
                // create contact in payload
                var contactPayload = new
                {
                  name = dto.AccountHolderName,
                  Type = "vendor",
                  reference_id = "canteen_" + realCanteenId
                };

                var contactContent = new StringContent(JsonSerializer.Serialize(contactPayload), Encoding.UTF8, "application/json");
                var contactResponse = await client.PostAsync("https://api.razorpay.com/v1/contacts", contactContent);

                if (!contactResponse.IsSuccessStatusCode)
                {
                    var error = await contactResponse.Content.ReadAsStringAsync();
                    return (false, $"Contact Creation Failed: {error}");
                }

                var contactJson = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync());
                string contactId = contactJson.RootElement.GetProperty("id").GetString() ?? "";

                // create fund accounts

                var fundAccountPayload = new
                {
                    contact_id = contactId,
                    account_type = "bank_account",
                    bank_account = new
                    {
                       
                        name = dto.AccountHolderName,
                        ifsc = dto.IFSC,
                        account_number = dto.AccountNumber


                    }
                };

                var fundContent = new StringContent(JsonSerializer.Serialize(fundAccountPayload), Encoding.UTF8, "application/json");
                var fundResponse = await client.PostAsync("https://api.razorpay.com/v1/fund_accounts", fundContent);

                if (fundResponse.IsSuccessStatusCode)
                {

                    var fundJson = JsonDocument.Parse(await fundResponse.Content.ReadAsStringAsync());
                    string razorpayFundAccountId = fundJson.RootElement.GetProperty("id").GetString() ?? "";

                    var accountEntity = new Canteenpaymentaccountsdetails
                    {
                        CanteenId = realCanteenId,
                        AccountHolderName = dto.AccountHolderName,
                        AccountNumber = dto.AccountNumber,
                        IFSC = dto.IFSC,
                        BankName = dto.BankName,
                        RazorpayAccountId = razorpayFundAccountId,
                        RazorpayFundAccountId = razorpayFundAccountId, // API se aayi hui ID
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                   
                    await _context.canteenpaymentaccountsdetails.AddAsync(accountEntity,cancellationToken);
                    await _context.SaveChangesAsync();

                    return (true, "Bank account successfully linked with Razorpay!");
                }
                var fundError = await fundResponse.Content.ReadAsStringAsync();
                return (false, $"Fund Account Creation Failed: {fundError}");
            }
            catch (Exception ex)
            {
                return (false, $"Internal Server Error: {ex.Message}");
            }


        }

    }
}
