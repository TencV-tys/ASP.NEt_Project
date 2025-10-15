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

            // Configure relationships
            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                .HasOne(b => b.Flight)
                .WithMany(f => f.Bookings)
                .HasForeignKey(b => b.FlightId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Flight entity
            builder.Entity<Flight>(entity =>
            {
                entity.HasIndex(f => f.FlightNumber).IsUnique();
                entity.Property(f => f.Price).HasPrecision(18, 2);
            });

            // Configure Booking entity
            builder.Entity<Booking>(entity =>
            {
                entity.Property(b => b.TotalAmount).HasPrecision(18, 2);
                entity.Property(b => b.Status).HasMaxLength(20);
            });
        }
    }
}