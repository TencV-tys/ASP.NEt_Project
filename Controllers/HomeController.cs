using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Data;
using AirlineReservationSystem.Models;
using AirlineReservationSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AirlineReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
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