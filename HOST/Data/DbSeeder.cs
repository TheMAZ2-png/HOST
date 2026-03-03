using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HOST.Constants;
using HOST.Models;


namespace HOST.Data
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndAdmin(IServiceProvider service)
        {
            var context = service.GetRequiredService<ApplicationDbContext>();
            
            var userManager = service.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            string[] roleNames = { Roles.Host.ToString(), Roles.Server.ToString(), Roles.Manager.ToString(), Roles.Guest.ToString() };
            
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Manager accounts are managed in the database and used during login.
        }
    }
}
