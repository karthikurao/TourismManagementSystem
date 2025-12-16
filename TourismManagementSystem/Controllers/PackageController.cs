using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tourism.DataAccess;
using Tourism.DataAccess.Models;
using TourismManagementSystem.Services;

namespace TourismManagementSystem.Controllers
{
    public class PackageController : Controller
    {
        private readonly TourismDbContext _context;
        private readonly IPackageValidationService _validationService;

        public PackageController(TourismDbContext context, IPackageValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        // GET: /Package
        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .OrderBy(p => p.StartDate)
                .ToListAsync();
            return View(packages);
        }

        // GET: /Package/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Initialize with default dates to ensure proper format
            var package = new Package
            {
                StartDate = DateTime.Today.AddDays(7), // Default to one week from today
                EndDate = DateTime.Today.AddDays(14)   // Default to two weeks from today
            };
            return View(package);
        }

        // POST: /Package/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Package package)
        {
            // Use validation service for business logic
            var validationErrors = _validationService.ValidatePackage(package, false);
            
            foreach (var error in validationErrors)
            {
                if (error.Contains("End date"))
                    ModelState.AddModelError("EndDate", error);
                else if (error.Contains("Start date"))
                    ModelState.AddModelError("StartDate", error);
                else
                    ModelState.AddModelError(string.Empty, error);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>())
                    .ToList();
                
                TempData["Error"] = string.Join("; ", errors);
                return View(package);
            }

            try
            {
                _context.Packages.Add(package);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Package '{package.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating package: " + ex.Message;
                return View(package);
            }
        }

        // GET: /Package/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound();
            return View(package);
        }

        // POST: /Package/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Package package)
        {
            // Use validation service for business logic
            var validationErrors = _validationService.ValidatePackage(package, true);
            
            foreach (var error in validationErrors)
            {
                if (error.Contains("End date"))
                    ModelState.AddModelError("EndDate", error);
                else if (error.Contains("Start date"))
                    ModelState.AddModelError("StartDate", error);
                else
                    ModelState.AddModelError(string.Empty, error);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Package '{package.Name}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await PackageExistsAsync(package.PackageId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error updating package: " + ex.Message;
                }
            }
            return View(package);
        }

        // GET: /Package/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var package = await _context.Packages
                .Include(p => p.Bookings)
                .FirstOrDefaultAsync(p => p.PackageId == id);
            if (package == null) return NotFound();

            // Use validation service to check if package can be deleted
            var canDelete = _validationService.CanPackageBeDeleted(package);
            ViewBag.HasActiveBookings = !canDelete;

            return View(package);
        }

        // POST: /Package/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages
                .Include(p => p.Bookings)
                .FirstOrDefaultAsync(p => p.PackageId == id);
            
            if (package != null)
            {
                // Use validation service to check if package can be deleted
                if (!_validationService.CanPackageBeDeleted(package))
                {
                    TempData["Error"] = "Cannot delete package with active bookings. Cancel all bookings first.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Package '{package.Name}' deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Package/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound();
            
            // Check if package is available for booking
            ViewBag.IsAvailableForBooking = _validationService.IsPackageAvailableForBooking(package);
            
            return View(package);
        }

        // GET: /Package/Search
        public async Task<IActionResult> Search(string? location, decimal? minPrice, decimal? maxPrice, DateTime? startDate)
        {
            var query = _context.Packages.AsQueryable();

            if (!string.IsNullOrEmpty(location))
                query = query.Where(p => p.Location.Contains(location));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (startDate.HasValue)
                query = query.Where(p => p.StartDate >= startDate.Value);

            var results = await query
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            ViewBag.SearchPerformed = true;
            ViewBag.SearchLocation = location;
            ViewBag.SearchMinPrice = minPrice;
            ViewBag.SearchMaxPrice = maxPrice;
            ViewBag.SearchStartDate = startDate?.ToString("yyyy-MM-dd");

            return View("Index", results);
        }

        private async Task<bool> PackageExistsAsync(int id)
        {
            return await _context.Packages.AnyAsync(e => e.PackageId == id);
        }
    }
}
