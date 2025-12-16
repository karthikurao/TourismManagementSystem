using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tourism.DataAccess.Models;
using Tourism.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace TourismManagementSystem.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<TourismDbContext>();

            // Ensure roles exist
            string[] roleNames = { "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default Admin user - CHANGE THESE CREDENTIALS BEFORE DEPLOYMENT
            string adminEmail = "admin@tourism.com"; // TODO: Change this email
            string adminPassword = "Admin@123456"; // TODO: Change this password before deployment

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Admin" // Set FullName to avoid NULL error
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create sample packages for testing
            await SeedSamplePackagesAsync(context);
        }

        private static async Task SeedSamplePackagesAsync(TourismDbContext context)
        {
            if (!await context.Packages.AnyAsync())
            {
                var packages = new List<Package>
                {
                    new Package
                    {
                        Name = "Goa Beach Paradise",
                        Description = "Experience the beautiful beaches of Goa with our comprehensive 4-day package. Includes visits to famous beaches, water sports, and local sightseeing.",
                        Location = "Goa",
                        Price = 8999m,
                        StartDate = DateTime.Now.AddDays(30),
                        EndDate = DateTime.Now.AddDays(34),
                        AvailableSeats = 20,
                        ImageUrl = "https://images.unsplash.com/photo-1559827260-dc66d52bef19?w=600&h=400&fit=crop"
                    },
                    new Package
                    {
                        Name = "Himalayan Adventure",
                        Description = "Trek through the magnificent Himalayas with experienced guides. Perfect for adventure enthusiasts looking for an unforgettable experience.",
                        Location = "Himachal Pradesh",
                        Price = 15999m,
                        StartDate = DateTime.Now.AddDays(45),
                        EndDate = DateTime.Now.AddDays(52),
                        AvailableSeats = 15,
                        ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600&h=400&fit=crop"
                    },
                    new Package
                    {
                        Name = "Rajasthan Heritage Tour",
                        Description = "Explore the royal heritage of Rajasthan with visits to magnificent palaces, forts, and cultural experiences in Jaipur, Udaipur, and Jodhpur.",
                        Location = "Rajasthan",
                        Price = 12999m,
                        StartDate = DateTime.Now.AddDays(60),
                        EndDate = DateTime.Now.AddDays(66),
                        AvailableSeats = 25,
                        ImageUrl = "https://images.unsplash.com/photo-1599661046289-e31897846e90?w=600&h=400&fit=crop"
                    },
                    new Package
                    {
                        Name = "Kerala Backwaters",
                        Description = "Relax in the serene backwaters of Kerala with houseboat stays, spice plantation visits, and traditional Ayurvedic treatments.",
                        Location = "Kerala",
                        Price = 10999m,
                        StartDate = DateTime.Now.AddDays(90),
                        EndDate = DateTime.Now.AddDays(95),
                        AvailableSeats = 18,
                        ImageUrl = "https://images.unsplash.com/photo-1602216056096-3b40cc0c9944?w=600&h=400&fit=crop"
                    }
                };

                context.Packages.AddRange(packages);
                await context.SaveChangesAsync();
            }
        }
    }
}
