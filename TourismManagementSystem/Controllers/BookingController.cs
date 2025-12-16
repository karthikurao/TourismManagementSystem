using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tourism.DataAccess;
using Tourism.DataAccess.Models;
using TourismManagementSystem.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.Controllers
{
    [Authorize] // Require authentication for all booking operations
    public class BookingController : Controller
    {
        private readonly TourismDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(TourismDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Booking/Create
        public async Task<IActionResult> Create(int? id)
        {
            System.Diagnostics.Debug.WriteLine($"=== BOOKING CREATE GET ===");
            System.Diagnostics.Debug.WriteLine($"Package ID: {id}");

            if (id == null)
            {
                TempData["Error"] = "Package ID is required.";
                return RedirectToAction("Index", "Package");
            }

            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction("Index", "Package");
            }

            System.Diagnostics.Debug.WriteLine($"Package found: {package.Name}");

            // Get current user's email to pre-populate the form
            var currentUser = await _userManager.GetUserAsync(User);
            var userEmail = currentUser?.Email ?? string.Empty;
            var userName = currentUser?.FullName ?? string.Empty;

            // Pass package info to view via ViewBag
            ViewBag.PackageId = package.PackageId;
            ViewBag.PackageName = package.Name;
            ViewBag.PackageLocation = package.Location;
            ViewBag.PackagePrice = package.Price;
            ViewBag.PackageImage = package.ImageUrl ?? "/images/default-package.jpg";

            // Create empty booking model with proper initialization
            var booking = new Booking
            {
                PackageId = package.PackageId,
                BookingDate = DateTime.Now,
                NumberOfSeats = 1,
                Status = "Pending", // Initial status
                CustomerName = userName,
                Email = userEmail,
                PhoneNumber = string.Empty
            };

            System.Diagnostics.Debug.WriteLine($"Booking model created with PackageId: {booking.PackageId}");

            return View(booking);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            System.Diagnostics.Debug.WriteLine($"=== BOOKING SUBMISSION ===");
            System.Diagnostics.Debug.WriteLine($"PackageId: {booking.PackageId}");
            System.Diagnostics.Debug.WriteLine($"CustomerName: '{booking.CustomerName}'");
            System.Diagnostics.Debug.WriteLine($"Email: '{booking.Email}'");
            System.Diagnostics.Debug.WriteLine($"PhoneNumber: '{booking.PhoneNumber}'");
            System.Diagnostics.Debug.WriteLine($"NumberOfSeats: {booking.NumberOfSeats}");

            // Get current user to associate with booking
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User authentication failed. Please login again.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Get package info first
            var package = await _context.Packages.FindAsync(booking.PackageId);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction("Index", "Package");
            }

            // Set ViewBag data for view reload
            ViewBag.PackageId = package.PackageId;
            ViewBag.PackageName = package.Name;
            ViewBag.PackageLocation = package.Location;
            ViewBag.PackagePrice = package.Price;
            ViewBag.PackageImage = package.ImageUrl ?? "/images/default-package.jpg";

            // Remove model state entries that might cause issues
            ModelState.Remove("Package");
            ModelState.Remove("Payments");
            ModelState.Remove("BookingId");

            // Use proper email validation
            if (!string.IsNullOrWhiteSpace(booking.Email))
            {
                var emailAttribute = new EmailAddressAttribute();
                if (!emailAttribute.IsValid(booking.Email))
                {
                    ModelState.AddModelError("Email", "Please enter a valid email address");
                }
            }

            // Business validation for seat availability
            if (package.AvailableSeats < booking.NumberOfSeats)
            {
                ModelState.AddModelError("NumberOfSeats", $"Only {package.AvailableSeats} seats available");
            }

            // If there are validation errors, display them
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>())
                    .ToList();
                
                TempData["Error"] = "Please fix the following errors: " + string.Join(", ", validationErrors);
                System.Diagnostics.Debug.WriteLine($"Validation errors: {string.Join(", ", validationErrors)}");
                return View(booking);
            }

            try
            {
                // Set booking details and associate with current user
                booking.BookingDate = DateTime.Now;
                booking.Status = "Pending"; // Changed from "Booked" to "Pending" until payment

                System.Diagnostics.Debug.WriteLine($"Attempting to save booking for user: {currentUser.Email}");

                // Save booking first (but don't update seats yet - wait for payment)
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Booking saved with ID: {booking.BookingId}");

                TempData["Success"] = $"Booking created! Your booking ID is #{booking.BookingId}. Please complete payment to confirm your booking.";
                
                // Redirect to payment instead of automatically confirming
                return RedirectToAction("Checkout", "Payment", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                
                TempData["Error"] = "Unable to process booking. Please try again. Error: " + ex.Message;
                return View(booking);
            }
        }

        // GET: /Booking/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            // Get current user to verify ownership
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            // Security check: Only allow users to view their own booking confirmations
            // For admin users, allow viewing all confirmations
            if (!User.IsInRole("Admin") && booking.Email != currentUser.Email)
            {
                TempData["Error"] = "You can only view your own bookings.";
                return RedirectToAction("MyBookings");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId);

            // If payment is not successful, redirect to payment page
            if (payment == null || payment.PaymentStatus != "Success")
            {
                TempData["Warning"] = "Payment is required to confirm your booking.";
                return RedirectToAction("Checkout", "Payment", new { bookingId = booking.BookingId });
            }

            var viewModel = new BookingViewModel
            {
                BookingId = booking.BookingId,
                PackageId = booking.PackageId,
                PackageName = booking.Package.Name,
                Location = booking.Package.Location,
                Price = booking.Package.Price,
                NumberOfSeats = booking.NumberOfSeats,
                BookingDate = booking.BookingDate,
                Status = booking.Status,
                CustomerName = booking.CustomerName,
                Email = booking.Email,
                PhoneNumber = booking.PhoneNumber,
                PaymentId = payment?.PaymentId,
                PaymentStatus = payment?.PaymentStatus ?? "Not Paid",
                Amount = payment?.Amount ?? 0,
                ImageUrl = booking.Package.ImageUrl ?? "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=600&q=80"
            };

            return View(viewModel);
        }

        // GET: /Booking/History
        public async Task<IActionResult> History()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Filter bookings to show only current user's bookings
            var history = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.Payments)
                .Where(b => b.Email == currentUser.Email) // Filter by current user's email
                .Select(b => new BookingViewModel
                {
                    BookingId = b.BookingId,
                    PackageId = b.PackageId,
                    PackageName = b.Package.Name,
                    Location = b.Package.Location,
                    Price = b.Package.Price,
                    NumberOfSeats = b.NumberOfSeats,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    PhoneNumber = b.PhoneNumber,
                    PaymentId = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.PaymentId : (int?)null,
                    PaymentStatus = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.PaymentStatus : "Not Paid",
                    Amount = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.Amount : 0,
                    ImageUrl = b.Package.ImageUrl ?? "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=600&q=80"
                })
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(history);
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Get bookings with package information and images - FILTERED BY CURRENT USER
            var myBookings = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.Payments)
                .Where(b => b.Email == currentUser.Email) // Filter by current user's email
                .Select(b => new BookingViewModel
                {
                    BookingId = b.BookingId,
                    PackageId = b.PackageId,
                    PackageName = b.Package.Name,
                    Location = b.Package.Location,
                    Price = b.Package.Price,
                    NumberOfSeats = b.NumberOfSeats,
                    BookingDate = b.BookingDate,
                    Status = b.Status,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    PhoneNumber = b.PhoneNumber,
                    PaymentId = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.PaymentId : (int?)null,
                    PaymentStatus = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.PaymentStatus : "Not Paid",
                    Amount = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.Amount : 0,
                    RefundAmount = b.Payments.FirstOrDefault() != null ? 
                        b.Payments.FirstOrDefault()!.RefundAmount : null,
                    ImageUrl = b.Package.ImageUrl ?? "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=600&q=80"
                })
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(myBookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // Get current user to verify ownership
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            
            if (booking == null) 
                return NotFound();

            // Security check: Only allow users to cancel their own bookings (unless admin)
            if (!User.IsInRole("Admin") && booking.Email != currentUser.Email)
            {
                TempData["Error"] = "You can only cancel your own bookings.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Booking is already cancelled.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Cancelled";

            // Restore available seats
            booking.Package.AvailableSeats += booking.NumberOfSeats;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId);
            
            if (payment != null && payment.PaymentStatus == "Success")
            {
                var refundAmount = payment.Amount * 0.85m; // 15% deduction
                var cancellationFee = payment.Amount * 0.15m;
                
                payment.PaymentStatus = "Refunded";
                payment.RefundAmount = refundAmount;
                
                TempData["Success"] = $"Booking cancelled successfully! Refund Details: Original Amount: ₹{payment.Amount:N2}, Cancellation Fee: ₹{cancellationFee:N2}, Refund Amount: ₹{refundAmount:N2}. Refund will be processed within 5-7 business days.";
            }
            else
            {
                TempData["Success"] = "Booking cancelled successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyBookings");
        }

        // GET: Booking/Test - Simple test method to verify database connection
        public async Task<IActionResult> Test()
        {
            try
            {
                var packageCount = await _context.Packages.CountAsync();
                var bookingCount = await _context.Bookings.CountAsync();
                
                ViewBag.Message = $"Database connection successful! Packages: {packageCount}, Bookings: {bookingCount}";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Database error: {ex.Message}";
                return View();
            }
        }
    }
}
