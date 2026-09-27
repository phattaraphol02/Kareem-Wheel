using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class RegisterVm
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        public string Customer_Name { get; set; } = "";

        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Customer_Email { get; set; } = "";

        [Required(ErrorMessage = "กรุณากำหนดรหัสผ่าน")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
        public string Password { get; set; } = "";

        public string? Address { get; set; }
        public string? Amphur_Id { get; set; }
        public string? Province_Id { get; set; }
        public string? PostalCode { get; set; }
        public string? Phone { get; set; }
        public string? District_Id { get; set; }
    }
}