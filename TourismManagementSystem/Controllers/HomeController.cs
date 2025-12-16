using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tourism.DataAccess;

namespace TourismManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly TourismDbContext _context;

        public HomeController(TourismDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Get featured packages (first 3)
            var featuredPackages = await _context.Packages
                .Where(p => p.AvailableSeats > 0)
                .OrderBy(p => p.StartDate)
                .Take(3)
                .ToListAsync();

            return View(featuredPackages);
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
