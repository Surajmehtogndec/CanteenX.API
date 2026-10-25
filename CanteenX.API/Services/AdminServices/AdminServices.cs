using CanteenX.API.Data;
using CanteenX.API.DTOs.Admin;
using CanteenX.API.DTOs.Admin.Paginations;
using CanteenX.API.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

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


        // yaha per user record pagination implmenet kiya gya hai 

        public async Task<UserDirectoryResponseDto> GetUserDirectoryAsync(UserDirectoryRequestDto request, CancellationToken cancellationToken = default)
        {
            // 1. Validate pagination inputs
            int pageNumber = Math.Max(1, request.PageNumber);
            int pageSize = Math.Clamp(request.PageSize, 1, 50);

            // 2. base read only query
            IQueryable<ApplicationUser> query = _context.Users.AsNoTracking();

            // 3. Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                // ToLower() removed for database index compatibility
                string search = request.SearchTerm.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // 4. Apply status filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => u.IsActive);
                }
                else if (request.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => !u.IsActive);
                }
            }

            // 5. filter by role                  
            if (!string.IsNullOrWhiteSpace(request.Role) &&
                !request.Role.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var userIdsWithRole =
                    from userRole in _context.UserRoles.AsNoTracking()
                    join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    where role.Name == request.Role
                    select userRole.UserId;

                query = query.Where(u => userIdsWithRole.Contains(u.Id));
            }

            // 6. Count records after filters, before pagination
            int totalRecords = await query.CountAsync(cancellationToken);

            // If no records found, return early to save database trips
            if (totalRecords == 0)
            {
                return new UserDirectoryResponseDto
                {
                    Items = new List<UserDirectoryItemDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = 0,
                    TotalPages = 0
                };
            }

            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            pageNumber = Math.Min(pageNumber, totalPages);

            //// 7. apply sorting
            bool descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            IOrderedQueryable<ApplicationUser> orderedQuery = request.SortBy?.Trim().ToLowerInvariant() switch
            {
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "createdat" => descending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "isactive" => descending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                _ => descending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)
            };

            orderedQuery = orderedQuery.ThenBy(u => u.Id); // Ensure stable ordering

            // 9. Fetch only the requested page (FIXED SKIP LOGIC)
            var users = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDirectoryItemDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,

                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 10. Fetch roles efficiently
            var userIds = users.Select(u => u.Id).ToList();

            var roles = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId) && role.Name != null
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name
                }
            ).ToListAsync(cancellationToken);

            // OPTIMIZATION: Use ILookup for O(1) fast mapping instead of O(N^2) Where() in a loop
            var rolesLookup = roles.ToLookup(r => r.UserId, r => r.RoleName);

            foreach (var user in users)
            {
                var userRoles = rolesLookup[user.Id];
                user.Role = userRoles.Any() ? string.Join(", ", userRoles) : "No Role";
            }



            return new UserDirectoryResponseDto
            {
                Items = users,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };
        }




        public async Task<IdentityResult?> UpdateUserDirectoryAsync(string userId, UserDirectoryItemDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentNullException.ThrowIfNull(dto);

            // 1. Fetch user
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            // Sanitize inputs
            string newFullName = dto.FullName?.Trim() ?? string.Empty;
            string newEmail = dto.Email?.Trim() ?? string.Empty;
            string? targetRole = dto.Role?.Trim();

            // 2. Use a Transaction so User update + Role update happen atomically
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 3. Only hit the database for User table if profile fields actually changed
                bool profileChanged =
                    !string.Equals(user.FullName, newFullName, StringComparison.Ordinal) ||
                    !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(user.UserName, newEmail, StringComparison.OrdinalIgnoreCase) ||
                    user.IsActive != dto.IsActive;

                if (profileChanged)
                {
                    user.FullName = newFullName;
                    user.Email = newEmail;
                    user.UserName = newEmail;
                    user.IsActive = dto.IsActive;

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return updateResult; // Rollback happens automatically on dispose
                    }
                }

                // 4. Optimize Role Update                                                                                                                                                               
                if (!string.IsNullOrWhiteSpace(targetRole))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);

                    // Check if the user already has ONLY this exact role (case-insensitive)
                    bool hasOnlyTargetRole = currentRoles.Count == 1 &&
                        currentRoles.Contains(targetRole, StringComparer.OrdinalIgnoreCase);

                    if (!hasOnlyTargetRole)
                    {
                        if (currentRoles.Count > 0)
                        {
                            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                            if (!removeResult.Succeeded)
                            {
                                return removeResult;
                            }
                        }

                        var addResult = await _userManager.AddToRoleAsync(user, targetRole);
                        if (!addResult.Succeeded)
                        {
                            return addResult;
                        }
                    }
                }

                // 5. Commit all changes together
                await transaction.CommitAsync(cancellationToken);
                return IdentityResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<IdentityResult?> DeleteUserDirectoryAsync(string userId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            var result = await _userManager.DeleteAsync(user);
            return result;
        }





    }

}
