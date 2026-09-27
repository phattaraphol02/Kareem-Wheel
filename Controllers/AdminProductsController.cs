using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environtment;

        public AdminProductsController(ApplicationDbContext context, IWebHostEnvironment environtment)
        {
            this.context = context;
            this.environtment = environtment;
        }

        public async Task<IActionResult> Index()
        {
            var products = await context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return View(products);
        }

        private void LoadDropdowns()
        {
            ViewBag.BrandList = new SelectList(context.Brands.OrderBy(b => b.Brand_Name).ToList(), "Brand_Id", "Brand_Name");
            ViewBag.CategoryList = new SelectList(context.Categories.OrderBy(c => c.Category_Name).ToList(), "Category_Id", "Category_Name");
        }

        public async Task<IActionResult> Create()
        {
            LoadDropdowns();
            return View(new ProductDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null) ModelState.AddModelError("ImageFile", "กรุณาเลือกรูปภาพ");
            if (productDto.Category_Id == 0) ModelState.AddModelError("Category_Id", "กรุณาเลือกหมวดหมู่");

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(productDto);
            }

            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssff") + Path.GetExtension(productDto.ImageFile!.FileName);
            string uploadPath = Path.Combine(environtment.WebRootPath, "Products");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            using (var stream = System.IO.File.Create(Path.Combine(uploadPath, newFileName)))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            Product product = new Product()
            {
                Product_Name = productDto.Product_Name,
                Brand_Id = productDto.Brand_Id,
                Category_Id = productDto.Category_Id,
                Price = productDto.Price,
                Description = productDto.Description,
                ImageFileName = newFileName,
                CreateAt = DateTime.Now,
                IsActive = true,
                Quantity = 0,
                Condition = productDto.Condition,
                CostPrice = productDto.CostPrice
            };

            context.Products.Add(product);
            context.SaveChanges();

            TempData["SuccessMessage"] = "เพิ่มสินค้าใหม่เรียบร้อยแล้ว";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var product = context.Products.Find(id);
            if (product == null) return RedirectToAction("Index");

            LoadDropdowns();

            var productDto = new ProductDto()
            {
                Product_Id = id,
                Product_Name = product.Product_Name,
                Brand_Id = product.Brand_Id,
                Category_Id = product.Category_Id,
                Price = product.Price,
                Description = product.Description,
                ImageFileName = product.ImageFileName,
                Condition = product.Condition,
                CostPrice = product.CostPrice
            };

            return View(productDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ProductDto productDto)
        {
            var product = context.Products.Find(id);
            if (product == null) return RedirectToAction("Index");

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(productDto);
            }

            if (productDto.ImageFile != null)
            {
                string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssff") + Path.GetExtension(productDto.ImageFile.FileName);
                string fullPath = Path.Combine(environtment.WebRootPath, "Products", newFileName);
                using (var stream = System.IO.File.Create(fullPath)) { productDto.ImageFile.CopyTo(stream); }

                if (!string.IsNullOrEmpty(product.ImageFileName))
                {
                    string oldPath = Path.Combine(environtment.WebRootPath, "Products", product.ImageFileName);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                product.ImageFileName = newFileName;
            }

            product.Product_Name = productDto.Product_Name;
            product.Brand_Id = productDto.Brand_Id;
            product.Category_Id = productDto.Category_Id;
            product.Price = productDto.Price;
            product.Description = productDto.Description;
            product.Condition = productDto.Condition;
            product.CostPrice = productDto.CostPrice;

            context.SaveChanges();
            TempData["SuccessMessage"] = "แก้ไขข้อมูลสินค้าเรียบร้อยแล้ว";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var product = context.Products.Find(id);
            if (product != null)
            {
                if (!string.IsNullOrWhiteSpace(product.ImageFileName))
                {
                    string imagePath = Path.Combine(environtment.WebRootPath, "Products", product.ImageFileName);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                context.Products.Remove(product);
                context.SaveChanges();
                TempData["Success"] = "ลบสินค้าเรียบร้อยแล้ว";
            }
            return RedirectToAction("Index");
        }

        // ฟังก์ชันสำหรับเปิด-ปิดสถานะสินค้า
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // สลับสถานะ (ถ้า true เป็น false, ถ้า false เป็น true)
            product.IsActive = !product.IsActive;

            context.Update(product);
            await context.SaveChangesAsync();

            TempData["Success"] = product.IsActive ? "เปิดจำหน่ายสินค้าเรียบร้อยแล้ว" : "ระงับการจำหน่ายสินค้าแล้ว (สินค้าหมด)";

            return RedirectToAction(nameof(Index));
        }
    }
}