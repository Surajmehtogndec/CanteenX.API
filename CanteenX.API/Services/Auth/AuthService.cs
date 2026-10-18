using CanteenX.API.Data;
using CanteenX.API.DTOs;
using CanteenX.API.Model;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CanteenX.API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        private readonly CanteenxdbContext _context;

        // Constants for magic strings
        private const string RoleStudent = "Student";
        private const string RoleCanteenStaff = "CanteenStaff";
        private const string ProviderGoogle = "Google";

        public AuthService(CanteenxdbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public Task<AuthResponseDto> RegisterAdminAsync(RegisterDto dto) =>
            RegisterUserInternalAsync(dto, RoleCanteenStaff);



        public Task<AuthResponseDto> RegisterAsync(RegisterDto dto) =>
            RegisterUserInternalAsync(dto, RoleStudent);



        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var loginValue = dto.EmailOrPhone.Trim();
            var user = loginValue.Contains('@')
                ? await _userManager.FindByEmailAsync(loginValue.ToLower())
                : _userManager.Users.FirstOrDefault(u => u.PhoneNumber == loginValue);

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return FailedAuthResponse("Invalid email/phone or password.");

            if (!user.IsActive)
                return FailedAuthResponse("Your account has been deactivated.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? RoleStudent;

            var token = await GenerateJwtTokenAsync(user, dto.RememberMe);

            // Generate and save Refresh Token
            var refeshtoken = await CreateAndSaveRefreshTokenAsync(user.Id);

            return SuccessAuthResponse("Login successful.", token, refeshtoken, user, role);
        }



        public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken)
        {
            var payload = await ValidateGoogleTokenAsync(idToken);
            if (payload?.Email == null) return null;

            var user = await _userManager.FindByEmailAsync(payload.Email);

            // Auto-register if user doesn't exist during Google Login
            if (user == null)
            {
                user = CreateNewApplicationUser(payload.Email, payload.Name);
                if (!(await _userManager.CreateAsync(user)).Succeeded) return null;
                await _userManager.AddToRoleAsync(user, RoleStudent);
            }

            if (!user.IsActive) return null;

            // Link Google account if not linked
            var existingLogin = await _userManager.FindByLoginAsync(ProviderGoogle, payload.Subject);
            if (existingLogin == null)
                await _userManager.AddLoginAsync(user, new UserLoginInfo(ProviderGoogle, payload.Subject, ProviderGoogle));

            var roles = await _userManager.GetRolesAsync(user);
            var token = await GenerateJwtTokenAsync(user);

            // Generate and save Refresh Token
            var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);

            return SuccessAuthResponse("Google login successful.", token, refreshToken, user, roles.FirstOrDefault() ?? RoleStudent);
        }

        public async Task<AuthResponseDto?> GoogleRegisterAsync(string idToken)
        {
            var payload = await ValidateGoogleTokenAsync(idToken);
            if (payload?.Email == null) return FailedAuthResponse("Invalid Google token or email not found.");

            if (await _userManager.FindByEmailAsync(payload.Email) != null)
                return FailedAuthResponse("Account with this Google email already exists. Please login.");

            var user = CreateNewApplicationUser(payload.Email, payload.Name);
            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
                return FailedAuthResponse(string.Join(", ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddLoginAsync(user, new UserLoginInfo(ProviderGoogle, payload.Subject, ProviderGoogle));
            var roleResult = await _userManager.AddToRoleAsync(user, RoleStudent);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return FailedAuthResponse("Unable to assign Student role.");
            }

            var token = await GenerateJwtTokenAsync(user);

            // Generate and save Refresh Token
            var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);
            return SuccessAuthResponse("Google registration successful.", token, refreshToken, user, RoleStudent);
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            //  User find karna
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            //  Secure Token generate karna
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);


            //  Frontend URL fetch karna (Configuration se)
            var frontendUrl = _configuration["AppSettings:FrontendUrl"]
                ?? throw new InvalidOperationException("Frontend URL is missing.");

            var resetLink = $"{frontendUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);
                }
                catch (Exception )
                {
                   
                }
            });
            

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(dto.Token), dto.NewPassword);
            return result.Succeeded;
        }

        public async Task<bool> ValidateResetTokenAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            return await _userManager.VerifyUserTokenAsync(
                user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);
        }






        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            // 1. Database se refresh token find karein (User data ke sath)
            var storedToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.TokenHash == dto.RefreshToken);

            // 2. Token ki validation check karein
            if (storedToken == null)
                return FailedAuthResponse("Invalid refresh token.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return FailedAuthResponse("Refresh token has expired. Please login again.");

            if (storedToken.RevokedAt != null)
                return FailedAuthResponse("Refresh token has been revoked. Security breach potential.");

            var user = storedToken.User;
            if (user == null || !user.IsActive)
                return FailedAuthResponse("User account is inactive or not found.");

            // 3. Purane token ko 'Revoke' (Cancel) karein (Isey Token Rotation kehte hain)
            storedToken.RevokedAt = DateTime.UtcNow;

            // 4. Naya JWT aur Naya Refresh Token banayein
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? RoleStudent;

            var newJwtToken = await GenerateJwtTokenAsync(user);
            var newRefreshToken = await CreateAndSaveRefreshTokenAsync(user.Id); // Aapka helper method

            // 5. Purane token mein likh dein ki uski jagah kaunsa naya token aaya hai
            storedToken.ReplacedByTokenHash = newRefreshToken;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            // 6. Naya response bhej dein
            return SuccessAuthResponse("Token refreshed successfully.", newJwtToken, newRefreshToken, user, role);
        }




        public async Task<bool> RevokeRefreshTokenAsync(string token)
        {
            // Database se wo specific token dhoondein
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == token);

            if (storedToken == null) return false;

            // Aap chahein toh RevokedAt update kar sakte hain, ya seedha Delete kar sakte hain.
            // Clean database ke liye hum isey delete kar dete hain:
            _context.RefreshTokens.Remove(storedToken);
            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // PRIVATE HELPER METHODS
        // =====================================================

        private async Task<AuthResponseDto> RegisterUserInternalAsync(RegisterDto dto, string role)
        {
            var email = dto.Email.Trim().ToLower();
            var phone = dto.PhoneNumber?.Trim();

            if (await _userManager.FindByEmailAsync(email) != null)
                return FailedAuthResponse("An account with this email already exists.");

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == phone);
                if (phoneExists)
                    return FailedAuthResponse("An account with this phone number already exists.");
            }
                

            
            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                UserName = email,
                PhoneNumber = phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return FailedAuthResponse(string.Join(", ", result.Errors.Select(e => e.Description)));

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return FailedAuthResponse($"Unable to assign role. {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }

            var token = await GenerateJwtTokenAsync(user);
            // Generate and save Refresh Token
            var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);

            return SuccessAuthResponse("Registration successful.", token, refreshToken, user, role);
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, bool rememberMe = false)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.FullName)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing.");
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256);
            var expiryMinutes = rememberMe ? 43200 : _configuration.GetValue<int>("Jwt:ExpiryMinutes"); // 43200 mins = 30 days

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
        {
            var clientId = _configuration["Authentication:Google:ClientId"]
                ?? throw new InvalidOperationException("Google ClientId is missing.");
            try
            {
                return await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"GOOGLE TOKEN ERROR: {ex.Message}");
                       
                return null;
            }
        }

        private ApplicationUser CreateNewApplicationUser(string email, string? name) => new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        private AuthResponseDto FailedAuthResponse(string message) => new()
        {
            Success = false,
            Message = message
        };

        private AuthResponseDto SuccessAuthResponse(string message, string token, string refreshToken, ApplicationUser user, string role) => new()
        {
            Success = true,
            Message = message,
            AccessToken = token,
            RefreshToken = refreshToken,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = role
            }
        };

        // NAYA METHOD: Secure Refresh Token generate aur save karne ke liye
        private async Task<string> CreateAndSaveRefreshTokenAsync(string userId)
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            var refreshTokenString = Convert.ToBase64String(randomNumber);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days validity
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshTokenString;
        }

    }
}