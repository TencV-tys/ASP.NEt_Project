using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.ViewModels
{
    public class AdminBookingsViewModel
    {
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public int TotalCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public string MinDate { get; set; } = string.Empty;
        public string MaxDate { get; set; } = string.Empty;
    }
} 