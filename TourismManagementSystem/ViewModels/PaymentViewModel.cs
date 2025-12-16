using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.ViewModels
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }
        
        [Display(Name = "Package Name")]
        public string PackageName { get; set; } = string.Empty;
        
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;
        
        [Display(Name = "Number of Seats")]
        public int NumberOfSeats { get; set; }
        
        [Display(Name = "Price per Seat")]
        [DataType(DataType.Currency)]
        public decimal PricePerSeat { get; set; }
        
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }
        
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;
        
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string PackageImageUrl { get; set; } = string.Empty;
    }
}