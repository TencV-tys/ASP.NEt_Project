using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Data;
using AirlineReservationSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AirlineReservationSystem.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: User/Flights with search - Available to all authenticated users
        [AllowAnonymous]
        public async Task<IActionResult> Flights(string departureCity, string arrivalCity, DateTime? departureDate, decimal? maxPrice, string sort = "departure_asc")
        {
            var query = _context.Flights
                .Where(f => f.IsActive && f.DepartureTime > DateTime.Now && f.AvailableSeats > 0)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(departureCity))
                query = query.Where(f => f.DepartureCity.Contains(departureCity));
            
            if (!string.IsNullOrEmpty(arrivalCity))
                query = query.Where(f => f.ArrivalCity.Contains(arrivalCity));
            
            if (departureDate.HasValue)
                query = query.Where(f => f.DepartureTime.Date == departureDate.Value.Date);
            
            if (maxPrice.HasValue)
                query = query.Where(f => f.Price <= maxPrice.Value);

            // Apply sorting
            query = sort switch
            {
                "price_asc" => query.OrderBy(f => f.Price),
                "price_desc" => query.OrderByDescending(f => f.Price),
                "departure_desc" => query.OrderByDescending(f => f.DepartureTime),
                "duration_asc" => query.OrderBy(f => (f.ArrivalTime - f.DepartureTime)),
                _ => query.OrderBy(f => f.DepartureTime) // default: departure_asc
            };

            var flights = await query.ToListAsync();
            return View(flights);
        }

        // GET: User/BookFlight/5 - Available to all authenticated users
        [AllowAnonymous]
        public async Task<IActionResult> BookFlight(Guid? id)
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
        public async Task<IActionResult> BookFlight(Guid flightId, int numberOfPassengers)
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
                BookingId = Guid.NewGuid(),
                UserId = userId!,
                FlightId = flightId,
                NumberOfPassengers = numberOfPassengers,
                TotalAmount = flight.Price * numberOfPassengers,
                Status = "Confirmed",
                BookingDate = DateTime.UtcNow
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

        // GET: User/EditBooking/5 
        public async Task<IActionResult> EditBooking(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            // Check if booking can be edited
            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Only confirmed bookings can be edited.";
                return RedirectToAction(nameof(MyBookings));
            }

            return View(booking);
        }

        // POST: User/EditBooking/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooking(Guid id, int numberOfPassengers, DateTime newDepartureTime, DateTime newArrivalTime)
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

            // Check if booking can be edited
            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Only confirmed bookings can be edited.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Validate new times
            if (newDepartureTime >= newArrivalTime)
            {
                TempData["Error"] = "Arrival time must be after departure time.";
                return View(booking);
            }

            if (newDepartureTime <= DateTime.Now)
            {
                TempData["Error"] = "Departure time must be in the future.";
                return View(booking);
            }

            // Check seat availability for passenger count change
            int seatDifference = numberOfPassengers - booking.NumberOfPassengers;
            if (seatDifference > 0 && booking.Flight.AvailableSeats < seatDifference)
            {
                TempData["Error"] = $"Not enough seats available. Only {booking.Flight.AvailableSeats} seats left.";
                return View(booking);
            }

            // Update the flight times and passenger count
            booking.Flight.DepartureTime = newDepartureTime;
            booking.Flight.ArrivalTime = newArrivalTime;
            booking.Flight.AvailableSeats -= seatDifference; // Update available seats
            booking.NumberOfPassengers = numberOfPassengers;
            booking.TotalAmount = booking.Flight.Price * numberOfPassengers;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking rescheduled successfully!";
            return RedirectToAction(nameof(MyBookings));
        }

        // POST: User/CancelBooking/5 - Only regular users can cancel their bookings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(Guid id)
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