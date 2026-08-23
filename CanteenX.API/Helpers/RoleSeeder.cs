using Microsoft.AspNetCore.Identity;
namespace CanteenX.API.Helpers
{
    public class RoleSeeder
    {
        private static readonly string[] Roles =
        {
            "Student",
            "CanteenStaff",
            "Admin"

        };

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
