using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Data;
using AirlineReservationSystem.Models;
using AirlineReservationSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AirlineReservationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // All actions in this controller are automatically protected by [Authorize(Roles = "Admin")]
        // No changes needed to individual actions

        public async Task<IActionResult> Dashboard()
        {
            var dashboardStats = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalFlights = await _context.Flights.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = await _context.Bookings.Where(b => b.Status == "Confirmed").SumAsync(b => b.TotalAmount),
                RecentBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Flight)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(dashboardStats);
        }

        // ... rest of the AdminController methods remain the same ...
    }
}