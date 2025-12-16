using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tourism.DataAccess.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        public Booking Booking { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Success, Pending, Failed, Refunded

        // NEW FIELD for cancelled/refund tracking
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        // Stripe-specific fields
        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [MaxLength(255)]
        public string? StripeSessionId { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } = "Stripe"; // Stripe, Cash, etc.

        // Stripe invoice fields
        [MaxLength(255)]
        public string? StripeInvoiceId { get; set; }

        [MaxLength(255)]
        public string? StripeCustomerId { get; set; }

        [MaxLength(500)]
        public string? StripeReceiptUrl { get; set; }
    }
}
