using Microsoft.AspNetCore.Identity;
using AirlineReservationSystem.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AirlineReservationSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminEmail = "admin@ars.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    EmailConfirmed = true
                };

                string adminPassword = "Admin123!";
                var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create sample regular user
            var userEmail = "user@ars.com";
            var regularUser = await userManager.FindByEmailAsync(userEmail);

            if (regularUser == null)
            {
                regularUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1995, 5, 15),
                    EmailConfirmed = true
                };

                string userPassword = "User123!";
                var createUser = await userManager.CreateAsync(regularUser, userPassword);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(regularUser, "User");
                }
            }

            // Seed sample flights with auto-generated flight numbers
            if (!context.Flights.Any())
            {
                context.Flights.AddRange(
                    new Flight
                    {
                        FlightNumber = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        Airline = "American Airlines",
                        DepartureCity = "New York",
                        ArrivalCity = "Los Angeles",
                        DepartureTime = DateTime.Now.AddDays(1),
                        ArrivalTime = DateTime.Now.AddDays(1).AddHours(6),
                        TotalSeats = 150,
                        AvailableSeats = 150,
                        Price = 299.99m
                    },
                    new Flight
                    {
                        FlightNumber = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        Airline = "United Airlines",
                        DepartureCity = "Chicago",
                        ArrivalCity = "Miami",
                        DepartureTime = DateTime.Now.AddDays(2),
                        ArrivalTime = DateTime.Now.AddDays(2).AddHours(3),
                        TotalSeats = 120,
                        AvailableSeats = 120,
                        Price = 199.99m
                    },
                    new Flight
                    {
                        FlightNumber = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        Airline = "Delta Airlines",
                        DepartureCity = "Atlanta",
                        ArrivalCity = "Seattle",
                        DepartureTime = DateTime.Now.AddDays(3),
                        ArrivalTime = DateTime.Now.AddDays(3).AddHours(5),
                        TotalSeats = 180,
                        AvailableSeats = 180,
                        Price = 349.99m
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}