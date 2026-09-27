using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    // บันทึกการเบิกอะไหล่ของพนักงาน — เบิกแล้วตัดสต็อกทันที ไม่ต้องรออนุมัติ
    public class SparePartRequisition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SparePart_Id { get; set; }

        [ForeignKey(nameof(SparePart_Id))]
        public SparePart? SparePart { get; set; }

        // รหัสพนักงานที่เบิก (Customer_Id ของ Role 2)
        [Required]
        public int Employee_Id { get; set; }

        [ForeignKey(nameof(Employee_Id))]
        public Customer? Employee { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "จำนวนที่เบิกต้องมากกว่า 0")]
        public int Qty { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        // งานที่นำอะไหล่ไปใช้ (ถ้าระบุ)
        public int? Appointment_Id { get; set; }

        [ForeignKey(nameof(Appointment_Id))]
        public Appointment? Appointment { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
