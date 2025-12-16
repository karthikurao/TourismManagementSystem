using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Tourism.DataAccess;
using Tourism.DataAccess.Models;
using TourismManagementSystem.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly TourismDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public PaymentController(TourismDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: /Payment/Checkout/{bookingId}
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Security check: Only allow users to pay for their own bookings
            if (!User.IsInRole("Admin") && booking.Email != currentUser.Email)
            {
                TempData["Error"] = "You can only pay for your own bookings.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId);

            if (existingPayment != null && existingPayment.PaymentStatus == "Success")
            {
                TempData["Info"] = "This booking has already been paid for.";
                return RedirectToAction("Confirmation", "Booking", new { id = booking.BookingId });
            }

            var viewModel = new PaymentViewModel
            {
                BookingId = booking.BookingId,
                PackageName = booking.Package.Name,
                Location = booking.Package.Location,
                NumberOfSeats = booking.NumberOfSeats,
                PricePerSeat = booking.Package.Price,
                TotalAmount = booking.NumberOfSeats * booking.Package.Price,
                CustomerName = booking.CustomerName,
                Email = booking.Email,
                PhoneNumber = booking.PhoneNumber,
                PackageImageUrl = booking.Package.ImageUrl ?? "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=600&q=80"
            };

            ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];

            return View(viewModel);
        }

        // POST: /Payment/CreateCheckoutSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(int bookingId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return Json(new { success = false, error = "Booking not found" });
            }

            // Security check
            if (!User.IsInRole("Admin") && booking.Email != currentUser.Email)
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            // Check if seats are still available
            if (booking.Package.AvailableSeats < booking.NumberOfSeats)
            {
                return Json(new { success = false, error = $"Sorry, only {booking.Package.AvailableSeats} seats are now available. Please modify your booking." });
            }

            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";
                var totalAmount = booking.NumberOfSeats * booking.Package.Price;

                System.Diagnostics.Debug.WriteLine($"Creating checkout session for booking {bookingId}, amount: {totalAmount}");

                // Create or retrieve Stripe customer
                string customerId = await CreateOrGetStripeCustomer(booking.Email, booking.CustomerName);

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(totalAmount * 100), // Stripe expects amount in cents
                                Currency = "inr", // Indian Rupees
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{booking.Package.Name} - {booking.Package.Location}",
                                    Description = $"Tourism booking for {booking.NumberOfSeats} seat(s)",
                                    Images = new List<string> { booking.Package.ImageUrl ?? "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=600&q=80" }
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = $"{domain}/Payment/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}&booking_id={bookingId}",
                    CancelUrl = $"{domain}/Payment/PaymentCancelled?booking_id={bookingId}",
                    InvoiceCreation = new SessionInvoiceCreationOptions
                    {
                        Enabled = true
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        {"booking_id", bookingId.ToString()},
                        {"customer_name", booking.CustomerName},
                        {"package_name", booking.Package.Name}
                    }
                };

                // Set either Customer ID or CustomerEmail, but not both
                if (!string.IsNullOrEmpty(customerId))
                {
                    options.Customer = customerId;
                    System.Diagnostics.Debug.WriteLine($"Using existing customer ID: {customerId}");
                }
                else
                {
                    options.CustomerEmail = booking.Email;
                    System.Diagnostics.Debug.WriteLine($"Using customer email: {booking.Email}");
                }

                var service = new SessionService();
                Session session = service.Create(options);

                System.Diagnostics.Debug.WriteLine($"Stripe session created successfully: {session.Id}");

                // Create or update payment record with pending status
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.BookingId == bookingId);

                if (payment == null)
                {
                    payment = new Tourism.DataAccess.Models.Payment
                    {
                        BookingId = bookingId,
                        Amount = totalAmount,
                        PaymentDate = DateTime.Now,
                        PaymentStatus = "Pending",
                        PaymentMethod = "Stripe",
                        StripeSessionId = session.Id,
                        StripeCustomerId = customerId
                    };
                    _context.Payments.Add(payment);
                }
                else
                {
                    payment.PaymentStatus = "Pending";
                    payment.StripeSessionId = session.Id;
                    payment.StripeCustomerId = customerId;
                    payment.PaymentDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, sessionId = session.Id });
            }
            catch (StripeException stripeEx)
            {
                System.Diagnostics.Debug.WriteLine($"Stripe API error: {stripeEx.Message}");
                return Json(new { success = false, error = $"Payment service error: {stripeEx.Message}" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error in CreateCheckoutSession: {ex.Message}");
                return Json(new { success = false, error = $"An error occurred: {ex.Message}" });
            }
        }

        private async Task<string?> CreateOrGetStripeCustomer(string email, string name)
        {
            try
            {
                var customerService = new CustomerService();
                
                // Check if customer already exists
                var customers = customerService.List(new CustomerListOptions
                {
                    Email = email,
                    Limit = 1
                });

                if (customers.Data.Any())
                {
                    var existingCustomer = customers.Data.First();
                    System.Diagnostics.Debug.WriteLine($"Found existing Stripe customer: {existingCustomer.Id}");
                    return existingCustomer.Id;
                }

                // Create new customer
                var customer = customerService.Create(new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    Description = "Tourism Management System customer"
                });

                System.Diagnostics.Debug.WriteLine($"Created new Stripe customer: {customer.Id}");
                return customer.Id;
            }
            catch (StripeException stripeEx)
            {
                System.Diagnostics.Debug.WriteLine($"Stripe error creating customer: {stripeEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error creating Stripe customer: {ex.Message}");
                return null;
            }
        }

        // GET: /Payment/PaymentSuccess
        public async Task<IActionResult> PaymentSuccess(string session_id, int booking_id)
        {
            try
            {
                var service = new SessionService();
                Session session = service.Get(session_id, new SessionGetOptions
                {
                    Expand = new List<string> { "invoice", "payment_intent" }
                });

                if (session.PaymentStatus == "paid")
                {
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.BookingId == booking_id);

                    if (payment != null)
                    {
                        payment.PaymentStatus = "Success";
                        payment.StripePaymentIntentId = session.PaymentIntentId;
                        payment.PaymentDate = DateTime.Now;

                        // Get Stripe customer information
                        if (!string.IsNullOrEmpty(session.CustomerId))
                        {
                            payment.StripeCustomerId = session.CustomerId;
                        }

                        // Get invoice information if available
                        if (session.Invoice != null)
                        {
                            payment.StripeInvoiceId = session.Invoice.Id;
                            payment.StripeReceiptUrl = session.Invoice.HostedInvoiceUrl;
                        }

                        // If no invoice, try to get receipt URL from charge
                        if (string.IsNullOrEmpty(payment.StripeReceiptUrl) && !string.IsNullOrEmpty(session.PaymentIntentId))
                        {
                            try
                            {
                                var chargeService = new ChargeService();
                                var charges = chargeService.List(new ChargeListOptions
                                {
                                    PaymentIntent = session.PaymentIntentId,
                                    Limit = 1
                                });
                                
                                if (charges.Data.Any())
                                {
                                    var charge = charges.Data.First();
                                    payment.StripeReceiptUrl = charge.ReceiptUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error retrieving charge receipt URL: {ex.Message}");
                            }
                        }

                        // Update booking status and reduce available seats
                        var booking = await _context.Bookings
                            .Include(b => b.Package)
                            .FirstOrDefaultAsync(b => b.BookingId == booking_id);
                        
                        if (booking != null)
                        {
                            booking.Status = "Confirmed";
                            
                            // Reduce available seats only after successful payment
                            booking.Package.AvailableSeats -= booking.NumberOfSeats;
                        }

                        await _context.SaveChangesAsync();

                        TempData["Success"] = "Payment successful! Your booking has been confirmed and seats have been reserved.";
                        return RedirectToAction("Confirmation", "Booking", new { id = booking_id });
                    }
                }

                TempData["Error"] = "Payment verification failed. Please contact support.";
                return RedirectToAction("Checkout", new { bookingId = booking_id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Payment verification error: {ex.Message}";
                return RedirectToAction("Checkout", new { bookingId = booking_id });
            }
        }

        // GET: /Payment/PaymentCancelled
        public async Task<IActionResult> PaymentCancelled(int booking_id)
        {
            // Update payment status to cancelled
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking_id);

            if (payment != null)
            {
                payment.PaymentStatus = "Cancelled";
                await _context.SaveChangesAsync();
            }

            TempData["Warning"] = "Payment was cancelled. You can try again when you're ready.";
            return RedirectToAction("Checkout", new { bookingId = booking_id });
        }

        // GET: /Payment/Receipt/{id}
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Package)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                TempData["Error"] = "Payment receipt not found.";
                return RedirectToAction("MyBookings", "Booking");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Security check
            if (!User.IsInRole("Admin") && payment.Booking.Email != currentUser.Email)
            {
                TempData["Error"] = "You can only view your own payment receipts.";
                return RedirectToAction("MyBookings", "Booking");
            }

            return View(payment);
        }

        // GET: /Payment/DownloadStripeReceipt/{paymentId}
        public async Task<IActionResult> DownloadStripeReceipt(int paymentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Security check
            if (!User.IsInRole("Admin") && payment.Booking.Email != currentUser.Email)
            {
                TempData["Error"] = "You can only access your own receipts.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Check if Stripe receipt URL exists
            if (string.IsNullOrEmpty(payment.StripeReceiptUrl))
            {
                TempData["Error"] = "Stripe receipt not available for this payment.";
                return RedirectToAction("Receipt", new { id = paymentId });
            }

            // Redirect to Stripe receipt URL (opens in new tab via JavaScript)
            return Redirect(payment.StripeReceiptUrl);
        }

        // GET: /Payment/GetStripeReceiptUrl/{paymentId} - For AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetStripeReceiptUrl(int paymentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                return Json(new { success = false, error = "Payment not found" });
            }

            // Security check
            if (!User.IsInRole("Admin") && payment.Booking.Email != currentUser.Email)
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            // Try to get receipt URL from database first
            if (!string.IsNullOrEmpty(payment.StripeReceiptUrl))
            {
                return Json(new { success = true, receiptUrl = payment.StripeReceiptUrl });
            }

            // If not in database, try to retrieve from Stripe
            try
            {
                string receiptUrl = await RetrieveStripeReceiptUrl(payment);
                
                if (!string.IsNullOrEmpty(receiptUrl))
                {
                    // Update database with found URL
                    payment.StripeReceiptUrl = receiptUrl;
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, receiptUrl = receiptUrl });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving Stripe receipt: {ex.Message}");
            }

            return Json(new { success = false, error = "Stripe receipt not available for this payment" });
        }

        private async Task<string> RetrieveStripeReceiptUrl(Tourism.DataAccess.Models.Payment payment)
        {
            try
            {
                // Method 1: Try to get from invoice
                if (!string.IsNullOrEmpty(payment.StripeInvoiceId))
                {
                    var invoiceService = new InvoiceService();
                    var invoice = invoiceService.Get(payment.StripeInvoiceId);
                    if (!string.IsNullOrEmpty(invoice.HostedInvoiceUrl))
                    {
                        return invoice.HostedInvoiceUrl;
                    }
                }

                // Method 2: Try to get from session
                if (!string.IsNullOrEmpty(payment.StripeSessionId))
                {
                    var sessionService = new SessionService();
                    var session = sessionService.Get(payment.StripeSessionId, new SessionGetOptions
                    {
                        Expand = new List<string> { "invoice" }
                    });

                    if (session.Invoice != null && !string.IsNullOrEmpty(session.Invoice.HostedInvoiceUrl))
                    {
                        // Update the invoice ID in our database
                        payment.StripeInvoiceId = session.Invoice.Id;
                        return session.Invoice.HostedInvoiceUrl;
                    }
                }

                // Method 3: Try to get from charges
                if (!string.IsNullOrEmpty(payment.StripePaymentIntentId))
                {
                    var chargeService = new ChargeService();
                    var charges = chargeService.List(new ChargeListOptions
                    {
                        PaymentIntent = payment.StripePaymentIntentId,
                        Limit = 1
                    });

                    if (charges.Data.Any() && !string.IsNullOrEmpty(charges.Data.First().ReceiptUrl))
                    {
                        return charges.Data.First().ReceiptUrl;
                    }
                }

                // Method 4: Try to create an invoice for the customer
                if (!string.IsNullOrEmpty(payment.StripeCustomerId) && !string.IsNullOrEmpty(payment.StripePaymentIntentId))
                {
                    return await CreateStripeInvoiceForPayment(payment);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RetrieveStripeReceiptUrl: {ex.Message}");
                return null;
            }
        }

        private async Task<string> CreateStripeInvoiceForPayment(Tourism.DataAccess.Models.Payment payment)
        {
            try
            {
                var invoiceService = new InvoiceService();
                var invoiceItemService = new InvoiceItemService();

                // Create invoice item
                var invoiceItem = invoiceItemService.Create(new InvoiceItemCreateOptions
                {
                    Customer = payment.StripeCustomerId,
                    Amount = (long)(payment.Amount * 100), // Convert to cents
                    Currency = "inr",
                    Description = $"Tourism booking payment - Booking ID: {payment.BookingId}",
                    Metadata = new Dictionary<string, string>
                    {
                        {"booking_id", payment.BookingId.ToString()},
                        {"payment_id", payment.PaymentId.ToString()}
                    }
                });

                // Create invoice
                var invoice = invoiceService.Create(new InvoiceCreateOptions
                {
                    Customer = payment.StripeCustomerId,
                    AutoAdvance = false,
                    CollectionMethod = "send_invoice",
                    DaysUntilDue = 30,
                    Description = "Tourism Management System Invoice",
                    Metadata = new Dictionary<string, string>
                    {
                        {"booking_id", payment.BookingId.ToString()},
                        {"payment_id", payment.PaymentId.ToString()}
                    }
                });

                // Finalize invoice
                invoice = invoiceService.FinalizeInvoice(invoice.Id);

                // Update payment record
                payment.StripeInvoiceId = invoice.Id;

                return invoice.HostedInvoiceUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating Stripe invoice: {ex.Message}");
                return null;
            }
        }

        // GET: /Payment/RefreshStripeReceipt/{paymentId} - Admin method to refresh receipt URLs
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefreshStripeReceipt(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                return Json(new { success = false, error = "Payment not found" });
            }

            try
            {
                string receiptUrl = await RetrieveStripeReceiptUrl(payment);
                
                if (!string.IsNullOrEmpty(receiptUrl))
                {
                    payment.StripeReceiptUrl = receiptUrl;
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = "Receipt URL refreshed successfully", receiptUrl = receiptUrl });
                }
                else
                {
                    return Json(new { success = false, error = "Could not retrieve receipt URL from Stripe" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: /Payment/TestStripeConnection - Test method to verify Stripe connection
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestStripeConnection()
        {
            try
            {
                var customerService = new CustomerService();
                var customers = customerService.List(new CustomerListOptions { Limit = 1 });
                
                return Json(new { 
                    success = true, 
                    message = "Stripe connection successful", 
                    customerCount = customers.Data.Count 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    suggestion = "Please check your Stripe API keys in appsettings.json"
                });
            }
        }

        // GET: /Payment/DebugPayment/{paymentId} - Debug method to check payment details
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DebugPayment(int paymentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                return Json(new { success = false, error = "Payment not found" });
            }

            // Security check
            if (!User.IsInRole("Admin") && payment.Booking.Email != currentUser.Email)
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            var debugInfo = new
            {
                PaymentId = payment.PaymentId,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                PaymentStatus = payment.PaymentStatus,
                PaymentMethod = payment.PaymentMethod,
                StripeSessionId = payment.StripeSessionId,
                StripePaymentIntentId = payment.StripePaymentIntentId,
                StripeCustomerId = payment.StripeCustomerId,
                StripeInvoiceId = payment.StripeInvoiceId,
                StripeReceiptUrl = payment.StripeReceiptUrl,
                HasReceiptUrl = !string.IsNullOrEmpty(payment.StripeReceiptUrl),
                PaymentDate = payment.PaymentDate
            };

            return Json(new { success = true, payment = debugInfo });
        }
    }
}
