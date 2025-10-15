using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Data;
using AirlineReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AirlineReservationSystem.Controllers
{
    [Authorize(Roles = "User")] // Only users with "User" role can access
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: User/Flights - Available to all authenticated users
        [AllowAnonymous]
        public async Task<IActionResult> Flights()
        {
            var flights = await _context.Flights
                .Where(f => f.IsActive && f.DepartureTime > DateTime.Now && f.AvailableSeats > 0)
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();

            return View(flights);
        }

        // GET: User/BookFlight/5 - Available to all authenticated users
        [AllowAnonymous]
        public async Task<IActionResult> BookFlight(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights.FindAsync(id);
            if (flight == null || !flight.IsActive || flight.AvailableSeats <= 0)
            {
                return NotFound();
            }

            return View(flight);
        }

        // POST: User/BookFlight - Only regular users can book flights
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookFlight(int flightId, int numberOfPassengers)
        {
            // Prevent admin users from booking flights
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    TempData["Error"] = "Admin users cannot book flights. Please use a regular user account.";
                    return RedirectToAction(nameof(Flights));
                }
            }

            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null || flight.AvailableSeats < numberOfPassengers)
            {
                TempData["Error"] = "Not enough seats available.";
                return RedirectToAction(nameof(Flights));
            }

            var userId = _userManager.GetUserId(User);
            var booking = new Booking
            {
                UserId = userId!,
                FlightId = flightId,
                NumberOfPassengers = numberOfPassengers,
                TotalAmount = flight.Price * numberOfPassengers,
                Status = "Confirmed"
            };

            // Update available seats
            flight.AvailableSeats -= numberOfPassengers;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Flight booked successfully!";
            return RedirectToAction(nameof(MyBookings));
        }

        // GET: User/MyBookings - Only regular users can view their bookings
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // POST: User/CancelBooking/5 - Only regular users can cancel their bookings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Check if user owns this booking
            var userId = _userManager.GetUserId(User);
            if (booking.UserId != userId)
            {
                return Forbid();
            }

            // Return seats to flight
            booking.Flight.AvailableSeats += booking.NumberOfPassengers;
            booking.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully!";
            return RedirectToAction(nameof(MyBookings));
        }

        [AllowAnonymous]
       public IActionResult AccessDenied()
       {
        return View();
       }
    }
   
}