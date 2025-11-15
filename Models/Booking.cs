using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirlineReservationSystem.Models
{
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        [Required]
        public string BookingReference { get; set; } = GenerateBookingReference(); // VAS- format

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public Guid FlightId { get; set; }

         public string PassengerName { get; set; } = string.Empty; 
         
        [Required]
        [Range(1, 10)]
        public int NumberOfPassengers { get; set; }
          
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed

        // Baggage Information
        [Required]
        [Range(0, 4)]
        public int CarryOnBags { get; set; } = 1; // Default 1 carry-on per passenger

        [Required]
        [Range(0, 4)]
        public int CheckedBags { get; set; } = 0; // Default 0 checked bags

        [NotMapped]
        public decimal BaggageFee => (CarryOnBags * CarryOnBagFee) + (CheckedBags * CheckedBagFee);

        [NotMapped]
        public decimal CarryOnBagFee => 0m; // First carry-on is usually free

        [NotMapped]
        public decimal CheckedBagFee => 30m; // $30 per checked bag

        // New fields for rescheduling
        public DateTime? RescheduledDepartureTime { get; set; }
        public DateTime? RescheduledArrivalTime { get; set; }

        [NotMapped]
        public bool IsRescheduled => RescheduledDepartureTime.HasValue && RescheduledArrivalTime.HasValue;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Flight Flight { get; set; } = null!;

        // Static method to generate booking reference
        private static string GenerateBookingReference()
        {
            var random = new Random();
            var number = random.Next(100000, 999999); // 6-digit number
            return $"VAS-{number}";
        }
    }
} 