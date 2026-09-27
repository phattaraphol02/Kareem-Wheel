using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    // รายจ่ายของร้านที่บันทึกเอง (ค่าน้ำค่าไฟ เงินเดือน ค่าเช่า ฯลฯ)
    // ต้นทุนสินค้าเข้าร้านคำนวณอัตโนมัติจากใบสั่งซื้อ ไม่ต้องบันทึกซ้ำที่นี่
    public class Expense
    {
        [Key]
        public int Expense_Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุรายการ")]
        [MaxLength(200)]
        [Display(Name = "รายการ")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกหมวดหมู่")]
        [MaxLength(50)]
        [Display(Name = "หมวดหมู่")]
        public string Category { get; set; } = "อื่นๆ";

        [Required(ErrorMessage = "กรุณาระบุจำนวนเงิน")]
        [Range(0.01, double.MaxValue, ErrorMessage = "จำนวนเงินต้องมากกว่า 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "จำนวนเงิน")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "วันที่")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [MaxLength(255)]
        [Display(Name = "หมายเหตุ")]
        public string? Note { get; set; }

        // ยอดรวมรายจ่ายต่อหมวด ใช้ส่งไปแสดงกราฟแท่งในหน้า AdminFinance
        public class CategoryTotal
        {
            public string Category { get; set; } = string.Empty;
            public decimal Total { get; set; }
        }

        public static readonly string[] Categories =
        {
            "ค่าเช่าร้าน",
            "ค่าน้ำ-ค่าไฟ",
            "เงินเดือนพนักงาน",
            "ค่าอะไหล่/วัสดุ",
            "ค่าขนส่ง",
            "ค่าการตลาด",
            "ค่าซ่อมบำรุง",
            "อื่นๆ"
        };
    }
}
