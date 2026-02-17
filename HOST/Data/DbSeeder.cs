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

            // Create Manager User if it doesn't exist
            var email = "manager@gmail.com";
            var userInDb = await userManager.FindByEmailAsync(email);
            
            if (userInDb == null)
            {
                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, "Manager@123");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Manager.ToString());
                }
                else
                {
                    var logger = service.GetRequiredService<ILogger<Program>>();
                    foreach (var error in createResult.Errors)
                    {
                        logger.LogError($"Error creating user: {error.Description}");
                    }
                }
            }
        }
    }
}
