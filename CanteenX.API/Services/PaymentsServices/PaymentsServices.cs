using CanteenX.API.Data;
using CanteenX.API.DTOs;
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
       
      

    }
}
