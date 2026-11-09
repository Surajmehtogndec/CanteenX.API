using CanteenX.API.Data;
using CanteenX.API.DTOs.CanteenOwner;
using CanteenX.API.Model;
using CanteenX.API.Services.CanteenOwnerService.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace CanteenX.API.Services.CanteenOwnerService
{
    public class CanteenOwnerService : ICanteenOwnerService
    {
        private readonly CanteenxdbContext _context;
        private readonly IWebHostEnvironment _environment;

        

        public CanteenOwnerService(CanteenxdbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
          
        }

        public async Task<int> GetCanteenIdByUserIdAsync(string userId)
        {
            // UserId ke basis par canteen fetch kar rahe hain
            var canteen = await _context.Canteens
                .AsNoTracking() // Performance ke liye kyunki hum sirf read kar rahe hain
                .FirstOrDefaultAsync(c => c.UserId == userId); // FK column name check kar lena

            // Agar canteen mil gayi toh uski ID bhejein, warna 0
            return canteen?.CanteenId ?? 0;
        }

        public async Task<bool> AddMenuItemAsync(AddMenuItemDto dto, CancellationToken cancellationToken)
        {
            try
            {
                bool canteenExists = await _context.Canteens
             .AnyAsync(c => c.CanteenId == dto.CanteenId, cancellationToken);

                if (!canteenExists)
                {
                    return false; // Canteen not found
                }
                // Handle image upload if provided
                string? imageUrl = null;
                if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                {
                    const int maxFileSize = 5 * 1024 * 1024;
                    var allowedExtensions = new[]
                    {
                    ".jpg",
                    ".jpeg",
                    ".png"
                };
                    var extension = Path.GetExtension(dto.ImageUrl?.FileName)
                                         ?.ToLowerInvariant();

                    // Extension aur Size ek hi if-condition mein check karein
                    if (string.IsNullOrEmpty(extension) ||
                        !allowedExtensions.Contains(extension) ||
                        dto.ImageUrl?.Length > maxFileSize)
                    {
                        return false;
                    }
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads","items");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    // 1. Item Name ko sanitize karna (special characters aur spaces hatane ke liye)
                    var sanitizedItemName = string.IsNullOrWhiteSpace(dto.ItemName)
                        ? "menuitems"
                        : new string(dto.ItemName.Where(char.IsLetterOrDigit).ToArray()).ToLower();

                    // 2. Current Date aur Time
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // 3. ItemName, CanteenId, aur Timestamp ko combine karke unique file name banana
                    var fileName = $"{sanitizedItemName}_{dto.CanteenId}_{timestamp}{extension}";
                    var filepath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filepath, FileMode.Create))
                    {
                        await dto.ImageUrl!.CopyToAsync(stream);
                    }
                    imageUrl = $"/uploads/items/{fileName}";

                }
                // Create a new MenuItem entity
                var menuItem = new MenuItems
                {
                    CanteenId = dto.CanteenId,
                    ItemName = dto.ItemName.Trim(),
                    Price = dto.Price.Trim(),
                    ImageUrl = imageUrl,
                    IsAvailable = dto.IsAvailable
                };
                // Add the new menu item to the database
               await _context.MenuItems.AddAsync(menuItem);
                await _context.SaveChangesAsync(cancellationToken);
                return true; // Successfully added the menu item
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                return false;
            }
            catch (Exception)
            {
                // Log the exception (you can use a logging framework here)
                // _logger.LogError(ex, "An error occurred while adding a menu item.");
                return false; // Indicate failure
            }
        }

        public async Task<CanteenProfileDto?> GetCanteenProfileDataAsync(string userId, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var profile = await (from user in _context.Users.AsNoTracking()
                                 join canteen in _context.Canteens.AsNoTracking()
                                 on user.Id equals canteen.UserId
                                 where user.Id == userId
                                 select new CanteenProfileDto
                                 {
                                     Id = user.Id,
                                     Email = user.Email ?? string.Empty,
                                     PhoneNumber = user.PhoneNumber ?? string.Empty,


                                     CanteenId = canteen.CanteenId,
                                     CanteenName = canteen.CanteenName,
                                     ImageUrl = canteen.ImageUrl ?? string.Empty,
                                     IsActive = canteen.IsActive ?? false

                                 }).FirstOrDefaultAsync(cancellationToken);

            return profile;
        }

    }
}
