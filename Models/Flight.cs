using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirlineReservationSystem.Models
{
    public class Flight
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid FlightId { get; set; } = Guid.NewGuid();

        [StringLength(50)]
        public string FlightNumber { get; set; } = string.Empty; // Will be auto-generated

        [Required]
        [StringLength(100)]
        public string Airline { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DepartureCity { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ArrivalCity { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        [Range(1, 1000)]
        public int TotalSeats { get; set; }

        [Required]
        [Range(0, 1000)]
        public int AvailableSeats { get; set; }

        [Required]
        [Range(0, 10000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}