using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    // แบนเนอร์หน้าแรก — เจ้าของร้านเพิ่ม/ปิดการใช้งาน/ลบได้เอง
    public class Banner
    {
        [Key]
        public int Banner_Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุหัวข้อ")]
        [MaxLength(150)]
        [Display(Name = "หัวข้อ")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "คำอธิบายใต้หัวข้อ")]
        public string? Subtitle { get; set; }

        [Required]
        [MaxLength(255)]
        public string ImageFileName { get; set; } = string.Empty;

        [Display(Name = "เปิดใช้งาน")]
        public bool IsActive { get; set; } = true;

        // เลขน้อยแสดงก่อน ใช้จัดลำดับตอนมีหลายแบนเนอร์
        [Display(Name = "ลำดับการแสดง")]
        public int SortOrder { get; set; } = 0;

        // ── ตั้งเวลาแสดง (ไม่บังคับ) ──
        // ว่างทั้งคู่ = แสดงตลอดไป
        [DataType(DataType.Date)]
        [Display(Name = "เริ่มแสดงวันที่")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "แสดงถึงวันที่")]
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // แบนเนอร์นี้ควรขึ้นหน้าเว็บตอนนี้หรือยัง
        [NotMapped]
        public bool IsLiveNow =>
            IsActive
            && (StartDate == null || StartDate.Value.Date <= DateTime.Today)
            && (EndDate == null || EndDate.Value.Date >= DateTime.Today);

        // ข้อความอธิบายสถานะช่วงเวลา ใช้โชว์ในหน้าจัดการ
        [NotMapped]
        public string ScheduleNote
        {
            get
            {
                if (StartDate == null && EndDate == null) return "แสดงตลอดไป";
                if (StartDate != null && StartDate.Value.Date > DateTime.Today)
                    return $"รอถึงวันที่ {StartDate.Value:dd MMM yyyy}";
                if (EndDate != null && EndDate.Value.Date < DateTime.Today)
                    return $"หมดอายุแล้ว ({EndDate.Value:dd MMM yyyy})";
                if (EndDate != null) return $"แสดงถึง {EndDate.Value:dd MMM yyyy}";
                return $"แสดงตั้งแต่ {StartDate!.Value:dd MMM yyyy}";
            }
        }
    }
}
