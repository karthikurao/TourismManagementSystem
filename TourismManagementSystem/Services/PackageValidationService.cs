using Tourism.DataAccess.Models;

namespace TourismManagementSystem.Services
{
    public interface IPackageValidationService
    {
        List<string> ValidatePackage(Package package, bool isEdit = false);
        bool IsPackageAvailableForBooking(Package package);
        bool CanPackageBeDeleted(Package package);
    }

    public class PackageValidationService : IPackageValidationService
    {
        public List<string> ValidatePackage(Package package, bool isEdit = false)
        {
            var errors = new List<string>();

            // Date validation
            if (package.EndDate <= package.StartDate)
            {
                errors.Add("End date must be after start date.");
            }

            // Only check future date for new packages or when editing start date
            if (!isEdit && package.StartDate <= DateTime.Today)
            {
                errors.Add("Start date must be in the future.");
            }

            // Business rules
            if (package.Duration < 1)
            {
                errors.Add("Package duration must be at least 1 day.");
            }

            if (package.Duration > 365)
            {
                errors.Add("Package duration cannot exceed 365 days.");
            }

            // Price validation
            if (package.Price <= 0)
            {
                errors.Add("Price must be greater than zero.");
            }

            return errors;
        }

        public bool IsPackageAvailableForBooking(Package package)
        {
            return package.AvailableSeats > 0 && 
                   package.StartDate > DateTime.Today &&
                   package.EndDate > package.StartDate;
        }

        public bool CanPackageBeDeleted(Package package)
        {
            return !package.Bookings.Any(b => b.Status == "Booked");
        }
    }
}