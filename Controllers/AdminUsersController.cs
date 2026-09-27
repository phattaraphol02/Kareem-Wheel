using CS66_WebApplication1.Services;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext context;

        public AdminUsersController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? role)
        {
            var allUsers = await context.Customers.ToListAsync();
            ViewBag.TotalCount = allUsers.Count;
            ViewBag.AdminCount = allUsers.Count(u => u.Role == 9);
            ViewBag.StaffCount = allUsers.Count(u => u.Role == 2);
            ViewBag.CustomerCount = allUsers.Count(u => u.Role == 1);

            // 💡 เพิ่ม Include District เพื่อให้หน้าตารางดึงตำบลไปโชว์ได้ด้วย (ถ้าต้องการ)
            var users = context.Customers
                .Include(c => c.Province)
                .Include(c => c.Amphur)
                .Include(c => c.District)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Customer_Name.Contains(searchString) || u.Customer_Email.Contains(searchString));
            }

            if (role.HasValue)
            {
                users = users.Where(u => u.Role == role.Value);
            }

            return View(await users.ToListAsync());
        }

        // เพิ่ม Action Details เพื่อแก้ปัญหา Error 404
        public async Task<IActionResult> Details(int id)
        {
            var user = await context.Customers
                .Include(c => c.Province)
                .Include(c => c.Amphur)
                .Include(c => c.District) // 💡 จุดสำคัญที่เพิ่มเข้ามา! เพื่อดึงชื่อตำบลมาแสดง
                .FirstOrDefaultAsync(u => u.Customer_Id == id);

            if (user == null) return NotFound();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAmphurs(string provinceId)
        {
            var pId = (provinceId ?? "").Trim();
            var amphurs = await context.Amphurs
                .AsNoTracking()
                .Where(a => a.Province_Id != null && a.Province_Id.ToString().Trim() == pId)
                .OrderBy(a => a.Amphur_Name)
                .Select(a => new { amphur_Id = a.Amphur_Id, amphur_Name = a.Amphur_Name })
                .ToListAsync();

            return Json(amphurs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Provinces = context.Provinces.OrderBy(p => p.Province_Name).ToList();
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            var emailExists = await context.Customers.AnyAsync(c => c.Customer_Email == model.Customer_Email);
            if (emailExists) ModelState.AddModelError("Customer_Email", "อีเมลนี้ถูกใช้งานแล้ว");

            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<Customer>();
                model.Password = hasher.HashPassword(null, model.Password);

                // ไม่จำเป็นต้อง Trim() ถ้าเป็นชนิดตัวเลข หรือเพื่อป้องกัน Error
                model.Province_Id = model.Province_Id;
                model.Amphur_Id = model.Amphur_Id;
                model.District_Id = model.District_Id; // เพิ่มการบันทึกตำบล

                context.Customers.Add(model);
                await context.SaveChangesAsync();
                TempData["Success"] = "สร้างผู้ใช้งานใหม่เรียบร้อยแล้ว";
                return RedirectToAction("Index");
            }
            ViewBag.Provinces = context.Provinces.OrderBy(p => p.Province_Name).ToList();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await context.Customers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Provinces = context.Provinces.OrderBy(p => p.Province_Name).ToList();

            if (user.Province_Id != null)
            {
                ViewBag.Amphurs = await context.Amphurs
                    .Where(a => a.Province_Id == user.Province_Id)
                    .OrderBy(a => a.Amphur_Name)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Amphurs = new List<Amphur>();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer user)
        {
            var userInDb = await context.Customers.FindAsync(user.Customer_Id);

            if (userInDb != null)
            {
                userInDb.Customer_Name = user.Customer_Name;
                userInDb.Customer_Email = user.Customer_Email;
                userInDb.Phone = user.Phone;
                userInDb.Role = user.Role;
                userInDb.Address = user.Address;

                // === อัปเดตข้อมูล จังหวัด อำเภอ และ "ตำบล" ===
                userInDb.Province_Id = user.Province_Id;
                userInDb.Amphur_Id = user.Amphur_Id;
                userInDb.District_Id = user.District_Id; // <-- จุดที่แก้ไข: เพิ่มบรรทัดนี้เพื่อเซฟตำบล!
                userInDb.PostalCode = user.PostalCode;

                // ถ้าระบุรหัสผ่านใหม่มา ค่อยเข้ารหัสและเซฟทับรหัสเดิม
                // แต่ถ้าส่งมาเป็นค่าว่าง (ปล่อยว่างหน้าฟอร์ม) มันจะข้ามจุดนี้ไป ทำให้รหัสผ่านเดิมไม่หายครับ
                if (!string.IsNullOrEmpty(user.Password))
                {
                    var hasher = new PasswordHasher<Customer>();
                    userInDb.Password = hasher.HashPassword(null, user.Password);
                }

                context.Update(userInDb);
                await context.SaveChangesAsync();
                TempData["Success"] = "อัปเดตข้อมูลผู้ใช้งานสำเร็จ";
                return RedirectToAction("Index");
            }
            ViewBag.Provinces = context.Provinces.OrderBy(p => p.Province_Name).ToList();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await context.Customers.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // สลับสถานะจริง/เท็จ
            user.IsActive = !user.IsActive;

            context.Update(user);
            await context.SaveChangesAsync();

            TempData["Success"] = $"เปลี่ยนสถานะของคุณ {user.Customer_Name} เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await context.Customers.FindAsync(id);
            if (user != null)
            {
                context.Customers.Remove(user);
                await context.SaveChangesAsync();
                TempData["Success"] = "ลบผู้ใช้งานสำเร็จ";
            }
            return RedirectToAction("Index");
        }
    }
}