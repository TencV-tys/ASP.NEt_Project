using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalFlights { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Booking> RecentBookings { get; set; } = new List<Booking>();
        
        // Add these new properties
        public int ConfirmedBookingsCount { get; set; }
        public int CancelledBookingsCount { get; set; }
        public int CompletedBookingsCount { get; set; }
        public int TodaysBookingsCount { get; set; }
    }
}