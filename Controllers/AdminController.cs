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

        public async Task<IActionResult> Dashboard()
        {
            // Update completed bookings first
            await UpdateCompletedBookings();
            
            var dashboardStats = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalFlights = await _context.Flights.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = await _context.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").SumAsync(b => b.TotalAmount),
                RecentBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Flight)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync(),
                // Add proper counts for all bookings
                ConfirmedBookingsCount = await _context.Bookings.CountAsync(b => b.Status == "Confirmed"),
                CancelledBookingsCount = await _context.Bookings.CountAsync(b => b.Status == "Cancelled"),
                CompletedBookingsCount = await _context.Bookings.CountAsync(b => b.Status == "Completed"),
                TodaysBookingsCount = await _context.Bookings.CountAsync(b => b.BookingDate.Date == DateTime.Today)
            };

            return View(dashboardStats);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(userRoles);
        }

        // POST: Admin/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            var result = await _userManager.AddToRoleAsync(user, role);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Role {role} assigned successfully to {user.Email}";
            }
            else
            {
                TempData["Error"] = "Failed to assign role";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent admin from deleting themselves
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account!";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete user";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Flights
        public async Task<IActionResult> Flights()
        {
            return View(await _context.Flights.OrderBy(f => f.DepartureTime).ToListAsync());
        }

        // GET: Admin/CreateFlight
        public IActionResult CreateFlight()
        {
            return View();
        }

        // POST: Admin/CreateFlight
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlight(Flight flight)
        {
            if (ModelState.IsValid)
            {
                // Validate departure time is in the future
                if (flight.DepartureTime <= DateTime.Now)
                {
                    ModelState.AddModelError("DepartureTime", "Departure time must be in the future.");
                    return View(flight);
                }

                flight.FlightId = Guid.NewGuid();
                // Auto-generate VAS- flight number
                var random = new Random();
                flight.FlightNumber = $"VAS-{random.Next(100000, 999999)}";
                flight.AvailableSeats = flight.TotalSeats;
                
                _context.Add(flight);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Flight created successfully! Flight Number: {flight.FlightNumber}";
                return RedirectToAction(nameof(Flights));
            }
            return View(flight);
        }

        // GET: Admin/EditFlight/5
        public async Task<IActionResult> EditFlight(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            // Check if this flight has any completed bookings
            var hasCompletedBookings = await _context.Bookings
                .AnyAsync(b => b.FlightId == id && b.Status == "Completed");

            // Check if flight has already departed or is today
            var hasDeparted = flight.DepartureTime <= DateTime.Now;
            var isToday = flight.DepartureTime.Date == DateTime.Today;

            // Pass this to the view
            ViewBag.HasCompletedBookings = hasCompletedBookings;
            ViewBag.HasDeparted = hasDeparted;
            ViewBag.IsToday = isToday;

            if (hasCompletedBookings)
            {
                TempData["Warning"] = "This flight has completed bookings. Editing may affect passenger records.";
            }

            if (hasDeparted)
            {
                TempData["Error"] = "This flight has already departed and cannot be edited.";
            }
            else if (isToday)
            {
                TempData["Warning"] = "This flight is scheduled for today. Editing is restricted.";
            }

            return View(flight);
        }

        // POST: Admin/EditFlight/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFlight(Guid id, Flight flight)
        {
            if (id != flight.FlightId)
            {
                return NotFound();
            }

            // Get the original flight to check departure time
            var originalFlight = await _context.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.FlightId == id);
            if (originalFlight == null)
            {
                return NotFound();
            }

            // Check if flight has already departed
            if (originalFlight.DepartureTime <= DateTime.Now)
            {
                TempData["Error"] = "Cannot edit flights that have already departed!";
                return RedirectToAction(nameof(Flights));
            }

            // Check if flight is scheduled for today
            if (originalFlight.DepartureTime.Date == DateTime.Today)
            {
                TempData["Error"] = "Cannot edit flights scheduled for today!";
                return RedirectToAction(nameof(Flights));
            }

            // Check if this flight has any completed bookings
            var hasCompletedBookings = await _context.Bookings
                .AnyAsync(b => b.FlightId == id && b.Status == "Completed");

            if (hasCompletedBookings)
            {
                TempData["Error"] = "Cannot edit flights with completed bookings! These flights have already been completed by passengers.";
                return RedirectToAction(nameof(Flights));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve the VAS- flight number
                    flight.FlightNumber = originalFlight.FlightNumber;
                    
                    _context.Update(flight);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Flight updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightExists(flight.FlightId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Flights));
            }
            return View(flight);
        }

        // POST: Admin/DeleteFlight/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFlight(Guid id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight != null)
            {
                // Check if flight has already departed
                if (flight.DepartureTime <= DateTime.Now)
                {
                    TempData["Error"] = "Cannot delete flights that have already departed!";
                    return RedirectToAction(nameof(Flights));
                }

                // Check if flight is scheduled for today
                if (flight.DepartureTime.Date == DateTime.Today)
                {
                    TempData["Error"] = "Cannot delete flights scheduled for today!";
                    return RedirectToAction(nameof(Flights));
                }

                // Check if this flight has any bookings
                var hasBookings = await _context.Bookings
                    .AnyAsync(b => b.FlightId == id);

                if (hasBookings)
                {
                    TempData["Error"] = "Cannot delete flights with existing bookings! You must cancel all bookings first.";
                    return RedirectToAction(nameof(Flights));
                }

                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Flight deleted successfully!";
            }
            return RedirectToAction(nameof(Flights));
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Bookings()
        {
            // Update completed bookings first
            await UpdateCompletedBookings();
            
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Admin/GetBookingDetails/5 - API endpoint for modal
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(Guid id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var result = new 
            {
                booking.BookingId,
                booking.BookingReference,
                booking.PassengerName,
                booking.NumberOfPassengers,
                booking.CarryOnBags,
                booking.CheckedBags,
                booking.TotalAmount,
                booking.BookingDate,
                booking.Status,
                booking.RescheduledDepartureTime,
                booking.RescheduledArrivalTime,
                User = new 
                {
                    booking.User.Email,
                    booking.User.FirstName,
                    booking.User.LastName
                },
                Flight = new 
                {
                    booking.Flight.FlightNumber,
                    booking.Flight.Airline,
                    booking.Flight.DepartureCity,
                    booking.Flight.ArrivalCity,
                    booking.Flight.DepartureTime,
                    booking.Flight.ArrivalTime,
                    booking.Flight.Price
                }
            };

            return Ok(result);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(Guid bookingId, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found!";
                return RedirectToAction(nameof(Bookings));
            }

            // Prevent editing completed bookings
            if (booking.Status == "Completed")
            {
                TempData["Error"] = "Cannot modify completed bookings! This flight has already been completed.";
                return RedirectToAction(nameof(Bookings));
            }

            // If changing from Cancelled to Confirmed, check seat availability
            if (booking.Status == "Cancelled" && status == "Confirmed")
            {
                if (booking.Flight.AvailableSeats < booking.NumberOfPassengers)
                {
                    TempData["Error"] = "Not enough seats available to confirm this booking!";
                    return RedirectToAction(nameof(Bookings));
                }
                booking.Flight.AvailableSeats -= booking.NumberOfPassengers;
            }
            // If changing from Confirmed to Cancelled, return seats
            else if (booking.Status == "Confirmed" && status == "Cancelled")
            {
                booking.Flight.AvailableSeats += booking.NumberOfPassengers;
            }

            booking.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking status updated to {status} successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        // GET: Admin/EditBookingStatus/5 - For modal or separate page
        public async Task<IActionResult> EditBookingStatus(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Prevent access to edit completed bookings
            if (booking.Status == "Completed")
            {
                TempData["Error"] = "Cannot edit completed bookings!";
                return RedirectToAction(nameof(Bookings));
            }

            return View(booking);
        }

        // POST: Admin/EditBookingStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBookingStatus(Guid id, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found!";
                return RedirectToAction(nameof(Bookings));
            }

            // Prevent editing completed bookings
            if (booking.Status == "Completed")
            {
                TempData["Error"] = "Cannot modify completed bookings! This flight has already been completed.";
                return RedirectToAction(nameof(Bookings));
            }

            // Handle seat availability based on status changes
            if (booking.Status == "Cancelled" && status == "Confirmed")
            {
                if (booking.Flight.AvailableSeats < booking.NumberOfPassengers)
                {
                    TempData["Error"] = "Not enough seats available to confirm this booking!";
                    return RedirectToAction(nameof(Bookings));
                }
                booking.Flight.AvailableSeats -= booking.NumberOfPassengers;
            }
            else if (booking.Status == "Confirmed" && status == "Cancelled")
            {
                booking.Flight.AvailableSeats += booking.NumberOfPassengers;
            }

            booking.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking status updated to {status} successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        // POST: Admin/RescheduleFlight
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleFlight(Guid bookingId, DateTime newDepartureTime, DateTime newArrivalTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found!";
                return RedirectToAction(nameof(Bookings));
            }

            // Prevent rescheduling completed bookings
            if (booking.Status == "Completed")
            {
                TempData["Error"] = "Cannot reschedule completed bookings! This flight has already been completed.";
                return RedirectToAction(nameof(Bookings));
            }

            // Update the booking with rescheduled times
            booking.RescheduledDepartureTime = newDepartureTime;
            booking.RescheduledArrivalTime = newArrivalTime;
            booking.Status = "Confirmed"; // Ensure it's confirmed after rescheduling

            await _context.SaveChangesAsync();

            TempData["Success"] = "Flight rescheduled successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        // Helper method to update completed bookings in database
        private async Task UpdateCompletedBookings()
        {
            var completedBookings = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.Status == "Confirmed" && 
                           (b.RescheduledArrivalTime.HasValue ? 
                            b.RescheduledArrivalTime.Value < DateTime.Now : 
                            b.Flight.ArrivalTime < DateTime.Now))
                .ToListAsync();

            foreach (var booking in completedBookings)
            {
                booking.Status = "Completed";
            }

            if (completedBookings.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private bool FlightExists(Guid id)
        {
            return _context.Flights.Any(e => e.FlightId == id);
        }

        // GET: Admin/BookingReceipt/5
        public async Task<IActionResult> BookingReceipt(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var receipt = new BookingReceiptViewModel
            {
                BookingReference = booking.BookingReference,
                FlightNumber = booking.Flight.FlightNumber,
                Airline = booking.Flight.Airline,
                DepartureCity = booking.Flight.DepartureCity,
                ArrivalCity = booking.Flight.ArrivalCity,
                DepartureTime = booking.Flight.DepartureTime,
                ArrivalTime = booking.Flight.ArrivalTime,
                RescheduledDepartureTime = booking.RescheduledDepartureTime,
                RescheduledArrivalTime = booking.RescheduledArrivalTime,
                PassengerName = booking.PassengerName,
                NumberOfPassengers = booking.NumberOfPassengers,
                CarryOnBags = booking.CarryOnBags,
                CheckedBags = booking.CheckedBags,
                BaseFare = booking.Flight.Price * booking.NumberOfPassengers,
                BaggageFee = booking.CheckedBags * 30m, // $30 per checked bag
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate,
                Status = booking.Status
            };

            return View(receipt);
        }

        // GET: Admin/PrintBookingReceipt/5
        public async Task<IActionResult> PrintBookingReceipt(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var receipt = new BookingReceiptViewModel
            {
                BookingReference = booking.BookingReference,
                FlightNumber = booking.Flight.FlightNumber,
                Airline = booking.Flight.Airline,
                DepartureCity = booking.Flight.DepartureCity,
                ArrivalCity = booking.Flight.ArrivalCity,
                DepartureTime = booking.Flight.DepartureTime,
                ArrivalTime = booking.Flight.ArrivalTime,
                RescheduledDepartureTime = booking.RescheduledDepartureTime,
                RescheduledArrivalTime = booking.RescheduledArrivalTime,
                PassengerName = booking.PassengerName,
                NumberOfPassengers = booking.NumberOfPassengers,
                CarryOnBags = booking.CarryOnBags,
                CheckedBags = booking.CheckedBags,
                BaseFare = booking.Flight.Price * booking.NumberOfPassengers,
                BaggageFee = booking.CheckedBags * 30m,
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate,
                Status = booking.Status
            }; 

            return View(receipt);
        } 
    }
}