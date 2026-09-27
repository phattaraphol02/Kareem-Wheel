using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models // เปลี่ยน namespace ให้ตรงกับโปรเจกต์ของคุณ
{
    public class District
    {
        [Key]
        public string District_Id { get; set; }

        public string District_Name { get; set; }

        public string Amphur_Id { get; set; }

        public string PostCodeMain { get; set; }

        // ส่วนเชื่อมความสัมพันธ์ (Navigation Property)
        [ForeignKey("Amphur_Id")]
        public virtual Amphur Amphur { get; set; }
    }
}