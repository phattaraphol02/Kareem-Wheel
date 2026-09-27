using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class SparePart
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่ออะไหล่")]
        [MaxLength(255, ErrorMessage = "ชื่ออะไหล่ยาวได้ไม่เกิน 255 ตัวอักษร")]
        [Display(Name = "ชื่ออะไหล่")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 9999999, ErrorMessage = "ราคาต้องไม่ติดลบ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ราคา")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "จำนวนในสต็อกต้องไม่ติดลบ")]
        [Display(Name = "จำนวนในสต็อก")]
        public int StockQuantity { get; set; }
    }
}
