using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class Province
    {
        [Key, Required, MaxLength(10)] // ปรับจาก 2 เป็น 10 ให้ตรงกับฐานข้อมูล
        public string Province_Id { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Province_Name { get; set; } = string.Empty;

        public ICollection<Amphur> Amphurs { get; set; } = new List<Amphur>();
    }
}