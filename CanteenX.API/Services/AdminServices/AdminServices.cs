using CanteenX.API.Data;
using CanteenX.API.DTOs.Admin;
using CanteenX.API.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CanteenX.API.Services.AdminServices
{
    public class AdminServices : IAdminServices
    {

        private readonly CanteenxdbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly UserManager<ApplicationUser> _userManager;

        public AdminServices(CanteenxdbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _environment = environment;
            _context = context;
            _userManager = userManager;
        }


        public async Task<List<CanteenStaffOptionDto>> GetAvailableCanteenStaffAsync()
        {

            // -------------------------------------------------
            // 1. Get all users having CanteenStaff role
            // -------------------------------------------------

            var user = await _userManager.GetUsersInRoleAsync("CanteenStaff");



            var availableStaff = user
               .Select(user => new CanteenStaffOptionDto
               {
                   UserId = user.Id,
                   CanteenName = user.FullName
               })
                .ToList();


            return availableStaff;

        }


        // add canteen post ka core logic implement kiya hai 
        public async Task<bool> AddCanteenAsync(CreateCanteenDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
            {
                return false;
            }

            var role = await _userManager.GetRolesAsync(user);

            if (!role.Contains("CanteenStaff"))
            {
                return false;
            }


            var existingCanteen = await _context.Canteens
                .FirstOrDefaultAsync(x => x.UserId == dto.UserId);


            if (existingCanteen != null)
            {
                return false;
            }

            string? imageFile = null;

            // image upload login implementation


            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
            {
                // allowed extensions

                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png"
                };



                var extension = Path.GetExtension(dto.ImageUrl?.FileName)
                     ?.ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {

                    return false;
                }
                if (dto.ImageUrl?.Length > 5 * 1024 * 1024)
                {
                    return false;
                }


                // wwwroot/uploads/canteens

                var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "canteens");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }


                var sanitizeName = string.IsNullOrWhiteSpace(user.FullName)
                    ? "canteen"
                    : new string(user.FullName.Where(char.IsLetterOrDigit).ToArray()).ToLower();

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");


                var fileName = $"{sanitizeName}_{timestamp}{extension}";

                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
           filePath,
           FileMode.Create))
                {
                    await dto.ImageUrl!.CopyToAsync(stream);
                }

                // Database mein URL save hoga
                imageFile =
                    $"/uploads/canteens/{fileName}";


            }


            // ==============================
            // CREATE CANTEEN
            // ==============================

            var canteen = new Canteen
            {
                CanteenName = user.FullName,
                UserId = user.Id,
                Location = dto.Location,
                Description = dto.Description,
                ImageUrl = imageFile,
                IsActive = dto.IsActive
            };

            _context.Canteens.Add(canteen);

            await _context.SaveChangesAsync();

            return true;


        }

        public async Task<UserProfileDtos?> GetProfileDataAsync(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            return new UserProfileDtos
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive
            };
        }


        public async Task<IdentityResult?> UpdateProfileDataAsync(string userId, UserProfileDtos dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }
            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;
            var result = await _userManager.UpdateAsync(user);
            return result;

        }
    }
}
