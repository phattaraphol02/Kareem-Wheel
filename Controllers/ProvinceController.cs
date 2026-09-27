using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    public class ProvinceController : Controller
    {
        private readonly ApplicationDbContext context;

        public ProvinceController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // Action สำหรับดึงรายชื่อจังหวัด
        public async Task<IActionResult> List()
        {
            var provinces = await context.Provinces.OrderBy(p => p.Province_Name).ToListAsync();
            return Json(provinces);
        }

        // Action สำหรับดึงอำเภอตามจังหวัด (แก้ไขให้ใช้ string ตาม Model ของคุณ)
        public IActionResult GetAmphurs(string provinceId)
        {
            var amphurs = context.Amphurs
                .Where(a => a.Province_Id == provinceId)
                .OrderBy(a => a.Amphur_Name)
                .ToList();

            return Json(amphurs);
        }
    }
}