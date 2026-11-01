using CanteenX.API.DTOs;
using CanteenX.API.Services.Auth;

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




        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            if (string.IsNullOrEmpty(model.RefreshToken))
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Refresh token is required." });
            }

            var result = await _authService.RefreshTokenAsync(model);

            if (!result.Success)
            {
                return Unauthorized(result); // 401 Unauthorized return karein taaki frontend user ko logout kar de
            }

            return Ok(result);
        }



        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto model)
        {
            if (string.IsNullOrEmpty(model.Token))
                return BadRequest(new { Message = "Token is required." });

            var result = await _authService.RevokeRefreshTokenAsync(model.Token);

            if (!result)
                return NotFound(new { Message = "Token not found." });

            return Ok(new { Message = "Token revoked successfully." });
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


        //Forgot Password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please enter a valid email address."
                });
            }

            var result = await _authService.ForgotPasswordAsync(dto.Email);
            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to send password reset email. Please check the email address and try again."
                });
            }
            return Ok(new
            {
                success = true,
                message = "Password reset email sent successfully. Please check your inbox."
            });
        }

        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetTokenAsync(
            [FromQuery] string email,
              [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid reset link."
                });
            }
            var result =
        await _authService.ValidateResetTokenAsync(
            email,
            token);
            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This password reset link has expired or is invalid."
                });
            }
            return Ok(new
            {
                success = true,
                message = "Reset link is valid."
            });

        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data."
                });
            }

            var result = await _authService.ResetPasswordAsync(dto);
            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to reset password. Please try again."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Password reset successfully."
            });
        }
    }
}
