using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminInventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminInventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // เรียงสินค้าที่เหลือน้อย/หมดก่อน ให้เห็นของที่ต้องจัดการด่วนอยู่บนสุด
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Quantity)
                .ToListAsync();

            return View(products);
        }
    }
}
