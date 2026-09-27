    using CS66_WebApplication1.Filters;
    using CS66_WebApplication1.Models;
    using CS66_WebApplication1.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;

    namespace CS66_WebApplication1.Controllers
    {
        public class ProductController : Controller
        {
            private readonly ApplicationDbContext context;
            private readonly IWebHostEnvironment environment;

        // 4. หน้าแสดงรายละเอียดสินค้า
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Product_Id == id);

            if (product == null) return NotFound();

            return View(product);
        }
        public ProductController(ApplicationDbContext context, IWebHostEnvironment environment)
            {
                this.context = context;
                this.environment = environment;
            }

            // 1. หน้า Index (แสดงสินค้า + รวมประเภทและแบรนด์)
            public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
            {
                var query = context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Product_Id)
                    .AsNoTracking();

                var totalItems = await query.CountAsync();
                var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;

                return View(items);
            }

            // 2. หน้าสร้างสินค้า (GET) — เฉพาะแอดมิน
            [AdminAuthorize]
            public IActionResult Create()
            {
                LoadSelectLists(); // เรียกใช้ Helper ใหม่ที่โหลดทั้ง Brand และ Category
                return View(new ProductDto());
            }

            // 3. หน้าสร้างสินค้า (POST) — เฉพาะแอดมิน
            [HttpPost]
            [AdminAuthorize]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(ProductDto productDto)
            {
                if (productDto.ImageFile == null)
                    ModelState.AddModelError("ImageFile", "กรุณาเลือกรูปภาพสินค้า");

                if (!ModelState.IsValid)
                {
                    LoadSelectLists();
                    return View(productDto);
                }

                // จัดการไฟล์รูปภาพ
                string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssff") + Path.GetExtension(productDto.ImageFile!.FileName);
                string uploadPath = Path.Combine(environment.WebRootPath, "Products");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                using (var stream = System.IO.File.Create(Path.Combine(uploadPath, newFileName)))
                {
                    await productDto.ImageFile.CopyToAsync(stream);
                }

                // บันทึกสินค้า (เพิ่ม Category_Id)
                Product product = new Product()
                {
                    Product_Name = productDto.Product_Name,
                    Brand_Id = productDto.Brand_Id,
                    Category_Id = productDto.Category_Id, // บันทึก Category
                    Price = productDto.Price,
                    Description = productDto.Description,
                    ImageFileName = newFileName,
                    CreateAt = DateTime.Now
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                TempData["Success"] = "เพิ่มสินค้าใหม่เรียบร้อยแล้ว";
                return RedirectToAction("Index");
            }

            // ✅ Helper Method รวมการโหลด Dropdown
            private void LoadSelectLists()
            {
                ViewBag.BrandList = new SelectList(context.Brands.OrderBy(b => b.Brand_Name), "Brand_Id", "Brand_Name");
                ViewBag.CategoryList = new SelectList(context.Categories.OrderBy(c => c.Category_Name), "Category_Id", "Category_Name");
            }
        }
    }