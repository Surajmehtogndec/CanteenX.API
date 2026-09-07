using CanteenX.API.DTOs;
using Google.Apis.Auth;
using CanteenX.API.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CanteenX.API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }


        // =====================================================
        // REGISTER STUDENT
        // =====================================================

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterDto dto)
        {
            // ---------------------------------------------
            // Check Email
            // ---------------------------------------------

            var email = dto.Email.Trim().ToLower();

            var existingEmailUser =
                await _userManager.FindByEmailAsync(email);

            if (existingEmailUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An account with this email already exists."
                };
            }


            // ---------------------------------------------
            // Check Phone Number
            // ---------------------------------------------

            var phoneNumber = dto.PhoneNumber.Trim();

            var existingPhoneUser =
                _userManager.Users
                    .FirstOrDefault(u =>
                        u.PhoneNumber == phoneNumber);

            if (existingPhoneUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An account with this phone number already exists."
                };
            }


            // ---------------------------------------------
            // Create ApplicationUser
            // ---------------------------------------------

            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),

                Email = email,

                UserName = email,

                PhoneNumber = phoneNumber,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };


            // ---------------------------------------------
            // Create User
            // Identity automatically hashes password
            // ---------------------------------------------

            var result =
                await _userManager.CreateAsync(
                    user,
                    dto.Password);


            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(e => e.Description));

                return new AuthResponseDto
                {
                    Success = false,
                    Message = errors
                };
            }


            // ---------------------------------------------
            // IMPORTANT:
            // Public registration = STUDENT ONLY
            // ---------------------------------------------

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Student");


            if (!roleResult.Succeeded)
            {
                // If role assignment fails,
                // remove the newly created user.

                await _userManager.DeleteAsync(user);

                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(e => e.Description));

                return new AuthResponseDto
                {
                    Success = false,
                    Message =
                        $"Unable to assign student role. {errors}"
                };
            }


            // ---------------------------------------------
            // Generate JWT
            // ---------------------------------------------

            var token =
                await GenerateJwtTokenAsync(user);


            // ---------------------------------------------
            // Response
            // ---------------------------------------------

            return new AuthResponseDto
            {
                Success = true,

                Message = "Registration successful.",

                AccessToken = token,

                User = new UserInfoDto
                {
                    Id = user.Id,

                    FullName = user.FullName,

                    Email = user.Email ?? string.Empty,

                    PhoneNumber = user.PhoneNumber,

                    Role = "Student"
                }
            };
        }


        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<AuthResponseDto> LoginAsync(
            LoginDto dto)
        {
            var loginValue =
                dto.EmailOrPhone.Trim();


            ApplicationUser? user = null;


            // ---------------------------------------------
            // Login using Email
            // ---------------------------------------------

            if (loginValue.Contains("@"))
            {
                user =
                    await _userManager.FindByEmailAsync(
                        loginValue.ToLower());
            }
            else
            {
                // -----------------------------------------
                // Login using Phone Number
                // -----------------------------------------

                user =
                    _userManager.Users
                        .FirstOrDefault(u =>
                            u.PhoneNumber == loginValue);
            }


            // ---------------------------------------------
            // User not found
            // ---------------------------------------------

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,

                    Message =
                        "Invalid email/phone or password."
                };
            }


            // ---------------------------------------------
            // Check Account Status
            // ---------------------------------------------

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,

                    Message =
                        "Your account has been deactivated."
                };
            }


            // ---------------------------------------------
            // Check Password
            // ---------------------------------------------

            var passwordValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    dto.Password);


            if (!passwordValid)
            {
                return new AuthResponseDto
                {
                    Success = false,

                    Message =
                        "Invalid email/phone or password."
                };
            }


            // ---------------------------------------------
            // Get User Roles
            // ---------------------------------------------

            var roles =
                await _userManager.GetRolesAsync(user);


            var role =
                roles.FirstOrDefault() ?? "Student";


            // ---------------------------------------------
            // Generate JWT
            // ---------------------------------------------

            var token =
                await GenerateJwtTokenAsync(
                    user,
                    dto.RememberMe);


            // ---------------------------------------------
            // Response
            // ---------------------------------------------

            return new AuthResponseDto
            {
                Success = true,

                Message = "Login successful.",

                AccessToken = token,

                User = new UserInfoDto
                {
                    Id = user.Id,

                    FullName = user.FullName,

                    Email = user.Email ?? string.Empty,

                    PhoneNumber = user.PhoneNumber,

                    Role = role
                }
            };
        }


        // =====================================================
        // GENERATE JWT TOKEN
        // =====================================================

        private async Task<string> GenerateJwtTokenAsync(
            ApplicationUser user,
            bool rememberMe = false)
        {
            // ---------------------------------------------
            // Get Roles
            // ---------------------------------------------

            var roles =
                await _userManager.GetRolesAsync(user);


            // ---------------------------------------------
            // Create Claims
            // ---------------------------------------------

            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email ?? string.Empty),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName)
            };


            // ---------------------------------------------
            // Add Roles to JWT
            // ---------------------------------------------

            foreach (var role in roles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }


            // ---------------------------------------------
            // JWT Key
            // ---------------------------------------------

            var jwtKey =
                _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is missing from appsettings.json.");
            }


            // ---------------------------------------------
            // Security Key
            // ---------------------------------------------

            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey));


            // ---------------------------------------------
            // Signing Credentials
            // ---------------------------------------------

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);


            // ---------------------------------------------
            // Expiry
            // ---------------------------------------------

            var expiryMinutes =
                _configuration.GetValue<int>(
                    "Jwt:ExpiryMinutes");


            // Remember Me = 30 Days
            if (rememberMe)
            {
                expiryMinutes = 60 * 24 * 30;
            }


            // ---------------------------------------------
            // Create Token
            // ---------------------------------------------

            var token =
                new JwtSecurityToken(
                    issuer:
                        _configuration["Jwt:Issuer"],

                    audience:
                        _configuration["Jwt:Audience"],

                    claims:
                        claims,

                    expires:
                        DateTime.UtcNow.AddMinutes(
                            expiryMinutes),

                    signingCredentials:
                        credentials
                );


            // ---------------------------------------------
            // Convert Token to String
            // ---------------------------------------------

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken)
        {
            var clientId =
                _configuration["Authentication:Google:ClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException(
                    "Google ClientId is missing from configuration.");
            }

            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[]
                        {
                    clientId
                        }
                    });
            }
            catch
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            // =====================================================
            // FIND EXISTING USER
            // =====================================================

            var user =
                await _userManager.FindByEmailAsync(payload.Email);


            // =====================================================
            // CREATE NEW STUDENT
            // =====================================================

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    FullName = payload.Name ?? string.Empty,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult =
                    await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    return null;
                }

                // Google registration = Student
                await _userManager.AddToRoleAsync(
                    user,
                    "Student");
            }


            // =====================================================
            // CHECK ACCOUNT STATUS
            // =====================================================

            if (!user.IsActive)
            {
                return null;
            }


            // =====================================================
            // ADD GOOGLE LOGIN
            // =====================================================

            var existingLogin =
                await _userManager.FindByLoginAsync(
                    "Google",
                    payload.Subject);

            if (existingLogin == null)
            {
                var loginInfo = new UserLoginInfo(
                    "Google",
                    payload.Subject,
                    "Google");

                await _userManager.AddLoginAsync(
                    user,
                    loginInfo);
            }


            // =====================================================
            // GET USER ROLE
            // =====================================================

            var roles =
                await _userManager.GetRolesAsync(user);

            var role =
                roles.FirstOrDefault() ?? "Student";


            // =====================================================
            // GENERATE CANTEENX JWT
            // =====================================================

            var accessToken =
              await  GenerateJwtTokenAsync(user);


            // =====================================================
            // RESPONSE
            // =====================================================

            return new AuthResponseDto
            {
                Success = true,

                Message = "Google login successful.",

                AccessToken = accessToken,

                User = new UserInfoDto
                {
                    Id = user.Id,

                    FullName = user.FullName ?? string.Empty,

                    Email = user.Email ?? string.Empty,

                    PhoneNumber = user.PhoneNumber,

                    Role = role
                }
            };
        }


        public async Task<AuthResponseDto?> GoogleRegisterAsync(string idToken)
        {
            // =====================================================
            // GOOGLE CLIENT ID
            // =====================================================

            var clientId =
                _configuration["Authentication:Google:ClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException(
                    "Google ClientId is missing from configuration.");
            }


            // =====================================================
            // VALIDATE GOOGLE ID TOKEN
            // =====================================================

            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload =
                    await GoogleJsonWebSignature.ValidateAsync(
                        idToken,
                        new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[]
                            {
                        clientId
                            }
                        });
            }
            catch
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid Google token."
                };
            }


            // =====================================================
            // GOOGLE EMAIL CHECK
            // =====================================================

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Google account email not found."
                };
            }


            // =====================================================
            // CHECK USER ALREADY EXISTS
            // =====================================================

            var existingUser =
                await _userManager.FindByEmailAsync(payload.Email);

            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message =
                        "An account with this Google email already exists. Please login instead."
                };
            }


            // =====================================================
            // CREATE APPLICATION USER
            // =====================================================

            var user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                EmailConfirmed = true,

                FullName =
                    string.IsNullOrWhiteSpace(payload.Name)
                        ? payload.Email.Split('@')[0]
                        : payload.Name,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };


            // =====================================================
            // CREATE USER
            // =====================================================

            var createResult =
                await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(
                        ", ",
                        createResult.Errors.Select(e => e.Description))
                };
            }


            // =====================================================
            // GOOGLE LOGIN PROVIDER
            // =====================================================

            var googleLogin =
                new UserLoginInfo(
                    "Google",
                    payload.Subject,
                    "Google");


            var loginResult =
                await _userManager.AddLoginAsync(
                    user,
                    googleLogin);


            if (!loginResult.Succeeded)
            {
                // Rollback user if Google login
                // information could not be added.

                await _userManager.DeleteAsync(user);

                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Unable to link Google account."
                };
            }


            // =====================================================
            // GOOGLE REGISTER = STUDENT
            // =====================================================

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Student");


            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Unable to assign Student role."
                };
            }


            // =====================================================
            // GENERATE CANTEENX JWT
            // =====================================================

            var accessToken =
                await GenerateJwtTokenAsync(user);


            // =====================================================
            // RESPONSE
            // =====================================================

            return new AuthResponseDto
            {
                Success = true,

                Message = "Google registration successful.",

                AccessToken = accessToken,

                User = new UserInfoDto
                {
                    Id = user.Id,

                    FullName =
                        user.FullName ?? string.Empty,

                    Email =
                        user.Email ?? string.Empty,

                    PhoneNumber =
                        user.PhoneNumber,

                    Role = "Student"
                }
            };
        }


        // password reset and forgot password methods would go here, but they are not included in the provided code snippet.


        public async Task<bool> ForgotPasswordAsync(string email)
        {
           var user  = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            // Generate a password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Encode Token For url
            var encodedToken = Uri.EscapeDataString(token);
           // get mvc application URl

            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ;
            if(string.IsNullOrWhiteSpace(frontendUrl))
            {
                throw new InvalidOperationException("Frontend URL is missing from configuration.");
            }

            // Create password reset link
            var resetLink =
                $"{frontendUrl}/Account/ResetPassword" +
                $"?email={Uri.EscapeDataString(user.Email!)}" +
                $"&token={encodedToken}";

            // Send reset email
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                resetLink
            );

            return true;
        }


        // reset password method

        public async  Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return false;
            }
            // Decode the token
            var decodedToken = Uri.UnescapeDataString(dto.Token);

            // Reset the password
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                return false;
            }
            return result.Succeeded;
        }


        // token lifetime configuration is already handled in Program.cs, so no need to implement it here.
        public async Task<bool> ValidateResetTokenAsync(string email, string token)
        {
            var user =
        await _userManager.FindByEmailAsync(email);
            if(user== null)
            {
                return false;
            }

            var result =
        await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            "ResetPassword",
            token);


            return result;
        }

    }
}