using CS66_WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Product
    {
        [Key]
        public int Product_Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อสินค้า")]
        [StringLength(500)]
        public string Product_Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Precision(16, 2)]
        public decimal Price { get; set; }

        [Precision(16, 2)]
        public decimal CostPrice { get; set; } = 0;

        [Required(ErrorMessage = "กรุณาระบุจำนวนสินค้า")]
        public int Quantity { get; set; } = 0;

        // Brand Relationship
        public int Brand_Id { get; set; }
        [ForeignKey(nameof(Brand_Id))]
        public Brand? Brand { get; set; }

        // Category Relationship
        public int Category_Id { get; set; }
        [ForeignKey(nameof(Category_Id))]
        public Category? Category { get; set; }

        [MaxLength(50)]
        public string Condition { get; set; } = "มือ 1";

        [MaxLength(255)]
        public string ImageFileName { get; set; } = "default.png";

        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // --- เพิ่มส่วนนี้ เพื่อให้เชื่อมโยงกับประวัติการสั่งซื้อได้ ---
        // ช่วยให้ Controller สามารถใช้ .Include(p => p.OrderDetails) เพื่อดึงสถิติได้
        public ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}