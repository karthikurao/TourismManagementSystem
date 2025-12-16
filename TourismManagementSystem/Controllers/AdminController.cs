using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tourism.DataAccess;
using TourismManagementSystem.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TourismDbContext _context;

        public AdminController(TourismDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var allBookings = await _context.Bookings
                    .Include(b => b.Package)
                    .Include(b => b.Payments)
                    .Select(b => new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        PackageId = b.PackageId,
                        PackageName = b.Package.Name,
                        Location = b.Package.Location,
                        NumberOfSeats = b.NumberOfSeats,
                        BookingDate = b.BookingDate,
                        Status = b.Status,
                        CustomerName = b.CustomerName,
                        Email = b.Email,
                        PhoneNumber = b.PhoneNumber,
                        PaymentStatus = b.Payments.Any() ? 
                            b.Payments.FirstOrDefault()!.PaymentStatus : "Not Paid",
                        Amount = b.Payments.Any() ? 
                            b.Payments.FirstOrDefault()!.Amount : 0,
                        RefundAmount = b.Payments.Any() ? 
                            b.Payments.FirstOrDefault()!.RefundAmount : null
                    })
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                // Summary stats
                var totalRevenue = await _context.Payments
                    .Where(p => p.PaymentStatus == "Success")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var totalRefunds = await _context.Payments
                    .Where(p => p.PaymentStatus == "Refunded")
                    .SumAsync(p => (decimal?)p.RefundAmount) ?? 0;

                var totalBookings = await _context.Bookings.CountAsync();
                var activeBookings = await _context.Bookings.CountAsync(b => b.Status == "Booked");

                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalRefunds = totalRefunds;
                ViewBag.TotalBookings = totalBookings;
                ViewBag.ActiveBookings = activeBookings;

                // Chart 1: Bookings per Package
                var bookingStats = await _context.Bookings
                    .Include(b => b.Package)
                    .GroupBy(b => b.Package.Name)
                    .Select(g => new { PackageName = g.Key, Count = g.Count() })
                    .ToListAsync();

                ViewBag.ChartLabels = bookingStats.Select(x => x.PackageName).ToArray();
                ViewBag.ChartData = bookingStats.Select(x => x.Count).ToArray();

                // Payment status distribution for pie chart
                var paidCount = await _context.Payments.CountAsync(p => p.PaymentStatus == "Success");
                var refundedCount = await _context.Payments.CountAsync(p => p.PaymentStatus == "Refunded");
                var failedCount = await _context.Payments.CountAsync(p => p.PaymentStatus == "Failed");
                var notPaidCount = totalBookings - paidCount - refundedCount - failedCount;

                ViewBag.PaidCount = paidCount;
                ViewBag.RefundedCount = refundedCount;
                ViewBag.FailedCount = failedCount;
                ViewBag.NotPaidCount = notPaidCount > 0 ? notPaidCount : 0;

                // Package statistics
                var totalPackages = await _context.Packages.CountAsync();
                var activePackages = await _context.Packages.CountAsync(p => p.AvailableSeats > 0);
                
                ViewBag.TotalPackages = totalPackages;
                ViewBag.ActivePackages = activePackages;

                return View(allBookings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading dashboard data: " + ex.Message;
                return View(new List<BookingViewModel>());
            }
        }

        // GET: Admin/Packages
        public async Task<IActionResult> Packages()
        {
            var packages = await _context.Packages
                .Include(p => p.Bookings)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(packages);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/BookingDetails/5
        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

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
                PaymentStatus = booking.Payments.Any() ? 
                    booking.Payments.FirstOrDefault()!.PaymentStatus : "Not Paid",
                Amount = booking.Payments.Any() ? 
                    booking.Payments.FirstOrDefault()!.Amount : 0
            };

            return View(viewModel);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Booking status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Booking not found.";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
