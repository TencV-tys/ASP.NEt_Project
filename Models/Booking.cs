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
        public string UserId { get; set; } = string.Empty;

        [Required]
        public Guid FlightId { get; set; }

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

        // New fields for rescheduling
        public DateTime? RescheduledDepartureTime { get; set; }
        public DateTime? RescheduledArrivalTime { get; set; }

           [NotMapped]
        public bool IsRescheduled => RescheduledDepartureTime.HasValue && RescheduledArrivalTime.HasValue;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Flight Flight { get; set; } = null!;
    }
}