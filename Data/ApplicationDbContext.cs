using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Flight> Flights { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Flight entity with GUID
            builder.Entity<Flight>(entity =>
            {
                entity.HasKey(f => f.FlightId);
                
                entity.HasIndex(f => f.FlightNumber).IsUnique();
                
                entity.Property(f => f.FlightId)
                      .ValueGeneratedOnAdd();
                
                entity.Property(f => f.Price)
                      .HasPrecision(18, 2);
                
                entity.Property(f => f.FlightNumber)
                      .HasMaxLength(50)
                      .IsRequired();
                
                entity.Property(f => f.Airline)
                      .HasMaxLength(100)
                      .IsRequired();
                
                entity.Property(f => f.DepartureCity)
                      .HasMaxLength(100)
                      .IsRequired();
                
                entity.Property(f => f.ArrivalCity)
                      .HasMaxLength(100)
                      .IsRequired();
            });

            // Configure Booking entity with GUID
            builder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.BookingId);
                
                entity.Property(b => b.BookingId)
                      .ValueGeneratedOnAdd();
                
                entity.Property(b => b.TotalAmount)
                      .HasPrecision(18, 2);
                
                entity.Property(b => b.Status)
                      .HasMaxLength(20);

                // Configure relationships
                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Flight)
                      .WithMany(f => f.Bookings)
                      .HasForeignKey(b => b.FlightId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}