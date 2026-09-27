using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class Category
    {
        [Key] // ระบุว่าเป็น Primary Key
        public int Category_Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อประเภทสินค้า")]
        [Display(Name = "ชื่อประเภทสินค้า")]
        public string Category_Name { get; set; } = string.Empty;

        // ความสัมพันธ์แบบ One-to-Many: 1 ประเภท มีได้หลายสินค้า
        public ICollection<Product>? Products { get; set; }
    }
}