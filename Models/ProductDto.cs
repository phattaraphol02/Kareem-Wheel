using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class ProductDto
    {
        [Required]
        public int Product_Id { get; set; }

        [Required, MaxLength(500)]
        public string Product_Name { get; set; } = "";

        [Required]
        public int Brand_Id { get; set; }

        // --- เพิ่มบรรทัดนี้ครับ ---
        [Required(ErrorMessage = "กรุณาเลือกประเภทสินค้า")]
        public int Category_Id { get; set; }
        // -----------------------

        public string? Brand_Name { get; set; }

        [Required]
        [Range(0.01, 9999999.99, ErrorMessage = "กรุณากรอกราคาให้ถูกต้อง")]
        public decimal Price { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "กรุณากรอกราคาทุนให้ถูกต้อง")]
        public decimal CostPrice { get; set; } = 0;

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = "";

        public string Condition { get; set; } = "มือ 1";

        public IFormFile? ImageFile { get; set; }

        public string? ImageFileName { get; set; }
    }
}