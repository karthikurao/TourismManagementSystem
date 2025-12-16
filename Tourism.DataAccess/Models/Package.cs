using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tourism.DataAccess.Models
{
    public class Package
    {
        [Key]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Package name is required")]
        [MaxLength(100, ErrorMessage = "Package name cannot exceed 100 characters")]
        [Display(Name = "Package Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Package Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        [Display(Name = "Destination")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between ₹0.01 and ₹999,999.99")]
        [Display(Name = "Price per Person (₹)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Available seats is required")]
        [Range(1, 100, ErrorMessage = "Available seats must be between 1 and 100")]
        [Display(Name = "Available Seats")]
        public int AvailableSeats { get; set; }

        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Package Image URL")]
        public string? ImageUrl { get; set; }

        // Navigation property
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Computed duration (not stored in DB)
        [NotMapped]
        public int Duration => (EndDate - StartDate).Days;
    }
}
