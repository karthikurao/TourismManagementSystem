using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tourism.DataAccess.Models;

namespace Tourism.DataAccess  
{
    public class TourismDbContext : IdentityDbContext<ApplicationUser>
    {
        public TourismDbContext(DbContextOptions<TourismDbContext> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // required for Identity tables

            // Configure Package -> Booking relationship properly
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Package)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PackageId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure Booking -> Payment relationship
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete for payments

            // Configure decimal precision
            modelBuilder.Entity<Package>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.RefundAmount)
                .HasColumnType("decimal(18,2)");

            // Configure string lengths and constraints
            modelBuilder.Entity<Package>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Package>()
                .Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired();

            modelBuilder.Entity<Package>()
                .Property(p => p.Location)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Booking>()
                .Property(b => b.CustomerName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Booking>()
                .Property(b => b.Email)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Booking>()
                .Property(b => b.PhoneNumber)
                .HasMaxLength(15)
                .IsRequired();

            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasMaxLength(20)
                .IsRequired();

            // Add indexes for better performance
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingDate);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.Email);

            modelBuilder.Entity<Package>()
                .HasIndex(p => p.Location);

            modelBuilder.Entity<Package>()
                .HasIndex(p => p.StartDate);
        }
    }

    // Design-time factory for migrations
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TourismDbContext>
    {
        public TourismDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TourismDbContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TourismDb;Trusted_Connection=True;MultipleActiveResultSets=true;");

            return new TourismDbContext(optionsBuilder.Options);
        }
    }
}
