using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Service
    {
        [Key]
        public int Service_Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อบริการ")]
        [Display(Name = "ชื่อบริการ")]
        public string Service_Name { get; set; }

        [Required(ErrorMessage = "กรุณาระบุราคา")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ราคา")]
        public decimal Price { get; set; }

        [Display(Name = "รายละเอียด")]
        public string Description { get; set; }

        [Display(Name = "ระยะเวลา (นาที)")]
        public int Duration { get; set; }
        public bool IsActive { get; set; } = true;
    }
}