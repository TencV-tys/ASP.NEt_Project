using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.ViewModels
{
    public class BookingReceiptViewModel
    {
        public string BookingReference { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public string DepartureCity { get; set; } = string.Empty;
        public string ArrivalCity { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? RescheduledDepartureTime { get; set; }
        public DateTime? RescheduledArrivalTime { get; set; }
        public string PassengerName { get; set; } = string.Empty; 
        public int NumberOfPassengers { get; set; }
        public int CarryOnBags { get; set; }
        public int CheckedBags { get; set; }
        public decimal BaseFare { get; set; }
        public decimal BaggageFee { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRescheduled => RescheduledDepartureTime.HasValue && RescheduledArrivalTime.HasValue;
    }
}