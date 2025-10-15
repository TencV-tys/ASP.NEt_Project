using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.ViewModels
{
    public class DashboardViewModel
    {
        public List<Booking> UserBookings { get; set; } = new List<Booking>();
        public List<Flight> AvailableFlights { get; set; } = new List<Flight>();
    }
}