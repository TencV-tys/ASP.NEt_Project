using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Data;
using AirlineReservationSystem.Models;
using AirlineReservationSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AirlineReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // Redirect to login if accessing home page without authentication
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // If user is authenticated, show the home page
                return View();
            }
            else
            {
                // If not authenticated, redirect to login
                return RedirectToAction("Login", "Account");
            }
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // Check if user is admin - if yes, redirect to admin dashboard
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
            }

            // Regular user dashboard logic
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userBookings = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var availableFlights = await _context.Flights
                .Where(f => f.IsActive && f.DepartureTime > DateTime.Now && f.AvailableSeats > 0)
                .OrderBy(f => f.DepartureTime)
                .Take(5)
                .ToListAsync();

            var dashboardModel = new DashboardViewModel
            {
                UserBookings = userBookings,
                AvailableFlights = availableFlights
            };

            return View(dashboardModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}