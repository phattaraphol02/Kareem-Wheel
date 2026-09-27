using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกบริการ")]
        public int Service_Id { get; set; }

        [ForeignKey("Service_Id")]
        public virtual Service? Service { get; set; }

        // ⚠️ แก้ไขตรงนี้ครับ: เปลี่ยนจาก string เป็น int ให้ตรงกับตาราง Customer
        [Required]
        public int Customer_Id { get; set; }

        [ForeignKey("Customer_Id")]
        public virtual Customer? Customer { get; set; }

        [Required(ErrorMessage = "กรุณาระบุวันที่และเวลา")]
        [Display(Name = "วันที่นัดหมาย")]
        [DataType(DataType.DateTime)]
        public DateTime Appointment_Date { get; set; }

        [StringLength(50)]
        [Display(Name = "สถานะ")]
        public string Status { get; set; } = "รออนุมัติ";

        [StringLength(500)]
        [Display(Name = "หมายเหตุ")]
        public string? Notes { get; set; }

        // ==========================================
        // 🔧 1. ส่วนเพิ่มสำหรับ "ระบบกระจายงานให้ช่าง"
        // ==========================================
        [StringLength(100)]
        [Display(Name = "รหัสช่างผู้รับผิดชอบ")]
        public string? Employee_Id { get; set; }

        // ==========================================
        // 🔔 2. ส่วนเพิ่มสำหรับ "ระบบกันลูกค้าเบี้ยวคิว"
        // ==========================================
        [Display(Name = "ส่งแจ้งเตือนแล้ว")]
        public bool IsReminderSent { get; set; } = false;

        [Display(Name = "ลูกค้ายืนยันคิวแล้ว")]
        public bool IsCustomerConfirmed { get; set; } = false;

        // ==========================================
        // 📧 3. ส่วนเพิ่มสำหรับ "ยืนยัน/ปฏิเสธคิวผ่านอีเมล" (ไม่ต้อง login)
        // ==========================================
        [StringLength(100)]
        [Display(Name = "โทเค็นยืนยันทางอีเมล")]
        public string? ConfirmToken { get; set; }

        [Display(Name = "วันหมดอายุโทเค็น")]
        public DateTime? TokenExpiry { get; set; }

        // ==========================================
        // 4. ส่วนชำระเงินค่าบริการ
        // ==========================================
        [MaxLength(255)]
        [Display(Name = "รูปสลิปค่าบริการ")]
        public string? ServicePaymentSlip { get; set; }

        [Display(Name = "วันที่ชำระค่าบริการ")]
        public DateTime? ServicePaidAt { get; set; }
    }
}