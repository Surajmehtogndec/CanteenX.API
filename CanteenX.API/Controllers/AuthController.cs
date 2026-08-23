using CanteenX.API.DTOs;
using CanteenX.API.Services.Auth;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace CanteenX.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        // =====================================================
        // REGISTER
        // POST: /api/auth/register
        // ===============================================


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result =
               await _authService.RegisterAsync(dto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    

    

    // =====================================================
    // LOGIN
    // POST: /api/auth/login
    // =====================================================

    [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }


        [HttpPost("google/Login")]
        public async Task<IActionResult> GoogleLogin(
    [FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
            {
                return BadRequest(new
                {
                    message = "Google ID token is required."
                });
            }

            var response =
                await _authService.GoogleLoginAsync(
                    dto.IdToken);

            if (response == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid Google login or inactive account."
                });
            }

            return Ok(response);
        }

        [HttpPost("google/register")]
        public async Task<IActionResult> GoogleRegister(
    [FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google ID token is required."
                });
            }

            var result =
                await _authService.GoogleRegisterAsync(
                    dto.IdToken);

            if (result == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google registration failed."
                });
            }

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
    }
