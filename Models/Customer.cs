using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Customer
    {
        [Key, Required]
        public int Customer_Id { get; set; }

        [Required]
        public string Customer_Name { get; set; } = string.Empty;

        [Required]
        public string Customer_Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int Role { get; set; } = 1;

        // เพิ่มฟิลด์สำหรับ เปิด/ปิด สถานะการใช้งานบัญชี
        [Required]
        public bool IsActive { get; set; } = true;

        // =======================================================
        // 💡 ปรับปรุงตรงนี้: เปลี่ยนเป็น string? และเอา [Required] ออกเพื่อรองรับค่า NULL
        // =======================================================

        public string? Address { get; set; }

        public string? Amphur_Id { get; set; }

        [ForeignKey(nameof(Amphur_Id))]
        public Amphur? Amphur { get; set; }

        public string? Province_Id { get; set; }

        [ForeignKey(nameof(Province_Id))]
        public Province? Province { get; set; }

        // 👇 แก้ไขตรงนี้เป็น string? ให้ตรงกับ Database (nvarchar(50)) 👇
        public string? District_Id { get; set; }

        // 👇 เพิ่ม Navigation Property เพื่อให้ Include(c => c.District) ทำงานได้ 👇
        [ForeignKey(nameof(District_Id))]
        public District? District { get; set; }

        public string? PostalCode { get; set; }

        public string? Phone { get; set; }
    }
}