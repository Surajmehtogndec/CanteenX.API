using CanteenX.API.DTOs;
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

      
        
    }
}
