using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext context;

        public AccountController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // =================================================================
        // 🔒 1. หน้าแสดงฟอร์มเข้าสู่ระบบ (GET: /Account/Login)
        // =================================================================
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (HttpContext.Session.GetInt32("Customer_Id").HasValue)
            {
                int? role = HttpContext.Session.GetInt32("Role");
                return role switch
                {
                    9 => RedirectToAction("Index", "AdminDashboard"),
                    2 => RedirectToAction("Index", "EmployeeDashboard"),
                    _ => RedirectToAction("Index", "CustomerDashboard")
                };
            }
            return View();
        }

        // =================================================================
        // 🔒 2. ระบบตรวจสอบการเข้าสู่ระบบ (POST: /Account/Login)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) return View(model);

            var email = model.Customer_Email?.Trim();
            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Customer_Email != null && c.Customer_Email.Trim() == email);

            if (customer == null)
            {
                ModelState.AddModelError("", "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
                return View(model);
            }

            // 🔥 บัญชีถูกระงับ: ส่งค่าแจ้งเตือน Error ไปที่หน้าเดิม
            if (!customer.IsActive)
            {
                TempData["ErrorMessage"] = "บัญชีของคุณถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ";
                return View(model);
            }

            var hasher = new PasswordHasher<Customer>();
            var result = hasher.VerifyHashedPassword(customer, customer.Password, model.Password);

            if (result == PasswordVerificationResult.Success)
            {
                HttpContext.Session.SetInt32("Customer_Id", customer.Customer_Id);
                HttpContext.Session.SetString("Customer_Name", customer.Customer_Name ?? "");
                HttpContext.Session.SetInt32("Role", customer.Role);

 

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.Customer_Id.ToString()),
                    new Claim(ClaimTypes.Name, customer.Customer_Name ?? ""),
                    new Claim(ClaimTypes.Role, customer.Role.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // ✨ บัญชีปกติ: เก็บข้อความแจ้งเตือนเมื่อล็อกอินสำเร็จ
                TempData["SuccessMessage"] = $"ยินดีต้อนรับคุณ {customer.Customer_Name} เข้าสู่ระบบสำเร็จ";

                // 🔥 ลบโค้ด returnUrl ออก เพื่อบังคับให้ระบบเด้งไปที่ Dashboard เสมอ
                return customer.Role switch
                {
                    9 => RedirectToAction("Index", "AdminDashboard"),
                    2 => RedirectToAction("Index", "EmployeeDashboard"),
                    _ => RedirectToAction("Index", "CustomerDashboard")
                };
            }

            ModelState.AddModelError("", "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            return View(model);
        }

        // =================================================================
        // 🔒 3. ระบบออกจากระบบ (GET: /Account/Logout)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =================================================================
        // ✨ 4. หน้าแสดงฟอร์มสมัครสมาชิก (GET: /Account/Register)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (HttpContext.Session.GetInt32("Customer_Id").HasValue)
            {
                int? role = HttpContext.Session.GetInt32("Role");
                return role switch
                {
                    9 => RedirectToAction("Index", "AdminDashboard"),
                    2 => RedirectToAction("Index", "EmployeeDashboard"),
                    _ => RedirectToAction("Index", "CustomerDashboard")
                };
            }

            ViewBag.Provinces = await context.Provinces.ToListAsync();
            return View();
        }

        // =================================================================
        // ✨ 5. ระบบรับข้อมูลสมัครสมาชิกไปบันทึกลง DB (POST: /Account/Register)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await context.Provinces.ToListAsync();
                return View(model);
            }

            var email = model.Customer_Email?.Trim();
            var isEmailExist = await context.Customers
                .AnyAsync(c => c.Customer_Email != null && c.Customer_Email.Trim() == email);

            if (isEmailExist)
            {
                ModelState.AddModelError("Customer_Email", "อีเมลนี้ถูกใช้งานในระบบแล้ว");
                ViewBag.Provinces = await context.Provinces.ToListAsync();
                return View(model);
            }

            var hasher = new PasswordHasher<Customer>();
            var newCustomer = new Customer
            {
                Customer_Name = model.Customer_Name,
                Customer_Email = model.Customer_Email,
                Phone = model.Phone,
                Address = model.Address,
                Province_Id = model.Province_Id,
                Amphur_Id = model.Amphur_Id,
                District_Id = model.District_Id,
                PostalCode = model.PostalCode,
                Role = 1,
                IsActive = true
            };

            newCustomer.Password = hasher.HashPassword(newCustomer, model.Password);

            try
            {
                context.Customers.Add(newCustomer);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "สมัครสมาชิกสำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบ";
                return RedirectToAction("Login", "Account");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "เกิดข้อผิดพลาดในการบันทึกข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            ViewBag.Provinces = await context.Provinces.ToListAsync();
            return View(model);
        }

        // =================================================================
        // 🛠️ 6. API ดึงข้อมูลอำเภอแบบสัมพันธ์กับจังหวัด (GET: /Account/GetAmphurs)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> GetAmphurs(string provinceId)
        {
            var amphurs = await context.Amphurs
                .Where(a => a.Province_Id == provinceId)
                .Select(a => new
                {
                    amphur_Id = a.Amphur_Id,
                    amphur_Name = a.Amphur_Name
                })
                .ToListAsync();

            return Json(amphurs);
        }

        // =================================================================
        // 🛠️ 7. API ดึงข้อมูลตำบลแบบสัมพันธ์กับอำเภอ (GET: /Account/GetDistricts)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> GetDistricts(string amphurId)
        {
            var districts = await context.Districts
                .Where(d => d.Amphur_Id == amphurId)
                .Select(d => new
                {
                    district_Id = d.District_Id,
                    district_Name = d.District_Name
                })
                .ToListAsync();

            return Json(districts);
        }
    }
}